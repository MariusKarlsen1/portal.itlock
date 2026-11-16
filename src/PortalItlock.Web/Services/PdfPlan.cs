using System.Runtime.InteropServices;
using System.Text;

namespace PortalItlock.Web.Services;

// Leser en plantegning (vektor-PDF fra tegneprogram) via PDFium: ord (også rotert
// tekst) og streker/buer, i samme koordinater som bildet portalen lager av side 1
// (punkter, origo øverst til venstre, sidens rotasjon tatt med).
public static class PdfPlan
{
    public readonly record struct P(double X, double Y)
    {
        public static P operator +(P a, P b) => new(a.X + b.X, a.Y + b.Y);
        public static P operator -(P a, P b) => new(a.X - b.X, a.Y - b.Y);
        public static P operator *(P a, double k) => new(a.X * k, a.Y * k);
        public double Len => Math.Sqrt(X * X + Y * Y);
        public static double Avst(P a, P b) => (a - b).Len;
    }

    public sealed record Ord(P Midt, double Storrelse, string Tekst);
    public sealed record Strek(P A, P B);
    public sealed record Side(double Bredde, double Hoyde, List<Ord> Ord, List<List<P>> Linjer, List<Strek> Streker);

    private const string Lib = "pdfium";

    [StructLayout(LayoutKind.Sequential)]
    private struct FsMatrix { public float A, B, C, D, E, F; }

    [DllImport(Lib)] private static extern void FPDF_InitLibrary();
    [DllImport(Lib)] private static extern IntPtr FPDF_LoadMemDocument(IntPtr data, int size, string? password);
    [DllImport(Lib)] private static extern void FPDF_CloseDocument(IntPtr doc);
    [DllImport(Lib)] private static extern IntPtr FPDF_LoadPage(IntPtr doc, int index);
    [DllImport(Lib)] private static extern void FPDF_ClosePage(IntPtr page);
    [DllImport(Lib)] private static extern double FPDF_GetPageWidth(IntPtr page);
    [DllImport(Lib)] private static extern double FPDF_GetPageHeight(IntPtr page);
    [DllImport(Lib)] private static extern int FPDF_PageToDevice(IntPtr page, int startX, int startY, int sizeX, int sizeY, int rotate, double pageX, double pageY, out int deviceX, out int deviceY);
    [DllImport(Lib)] private static extern IntPtr FPDFText_LoadPage(IntPtr page);
    [DllImport(Lib)] private static extern void FPDFText_ClosePage(IntPtr textPage);
    [DllImport(Lib)] private static extern int FPDFText_CountChars(IntPtr textPage);
    [DllImport(Lib)] private static extern uint FPDFText_GetUnicode(IntPtr textPage, int index);
    [DllImport(Lib)] private static extern int FPDFText_GetCharBox(IntPtr textPage, int index, out double left, out double right, out double bottom, out double top);
    [DllImport(Lib)] private static extern double FPDFText_GetFontSize(IntPtr textPage, int index);
    [DllImport(Lib)] private static extern int FPDFPage_CountObjects(IntPtr page);
    [DllImport(Lib)] private static extern IntPtr FPDFPage_GetObject(IntPtr page, int index);
    [DllImport(Lib)] private static extern int FPDFPageObj_GetType(IntPtr obj);
    [DllImport(Lib)] private static extern int FPDFPageObj_GetMatrix(IntPtr obj, out FsMatrix matrix);
    [DllImport(Lib)] private static extern int FPDFFormObj_CountObjects(IntPtr form);
    [DllImport(Lib)] private static extern IntPtr FPDFFormObj_GetObject(IntPtr form, uint index);
    [DllImport(Lib)] private static extern int FPDFPath_CountSegments(IntPtr path);
    [DllImport(Lib)] private static extern IntPtr FPDFPath_GetPathSegment(IntPtr path, int index);
    [DllImport(Lib)] private static extern int FPDFPathSegment_GetPoint(IntPtr segment, out float x, out float y);
    [DllImport(Lib)] private static extern int FPDFPathSegment_GetType(IntPtr segment);

    private const int ObjPath = 2, ObjForm = 5;
    private const int SegLine = 0, SegBezier = 1, SegMove = 2;
    private const int Skala = 10; // FPDF_PageToDevice gir heltall - regn i tideler av punkter

    private static bool _init;

    public static Side LesSide(byte[] pdf, int sideIndex = 0)
    {
        lock (PdfTekst.Laas)
        {
            if (!_init)
            {
                FPDF_InitLibrary();
                _init = true;
            }
            var handle = GCHandle.Alloc(pdf, GCHandleType.Pinned);
            var doc = IntPtr.Zero;
            var side = IntPtr.Zero;
            try
            {
                doc = FPDF_LoadMemDocument(handle.AddrOfPinnedObject(), pdf.Length, null);
                if (doc == IntPtr.Zero) throw new InvalidOperationException("Klarte ikke å åpne PDF-en.");
                side = FPDF_LoadPage(doc, sideIndex);
                if (side == IntPtr.Zero) throw new InvalidOperationException("Fant ikke side 1 i PDF-en.");

                var w = FPDF_GetPageWidth(side);
                var h = FPDF_GetPageHeight(side);
                var pg = side;
                P Til(double x, double y)
                {
                    FPDF_PageToDevice(pg, 0, 0, (int)(w * Skala), (int)(h * Skala), 0, x, y, out var dx, out var dy);
                    return new P(dx / (double)Skala, dy / (double)Skala);
                }

                var ord = LesOrd(side, Til);
                var linjer = new List<List<P>>();
                var streker = new List<Strek>();
                var n = FPDFPage_CountObjects(side);
                for (var i = 0; i < n; i++)
                {
                    LesObjekt(FPDFPage_GetObject(side, i), Identitet, Til, linjer, streker, 0);
                }
                return new Side(w, h, ord, linjer, streker);
            }
            finally
            {
                if (side != IntPtr.Zero) FPDF_ClosePage(side);
                if (doc != IntPtr.Zero) FPDF_CloseDocument(doc);
                handle.Free();
            }
        }
    }

    private static readonly FsMatrix Identitet = new() { A = 1, D = 1 };

    private static FsMatrix Gang(FsMatrix forelder, FsMatrix barn) => new()
    {
        // barn først, så forelder: p' = forelder(barn(p))
        A = barn.A * forelder.A + barn.B * forelder.C,
        B = barn.A * forelder.B + barn.B * forelder.D,
        C = barn.C * forelder.A + barn.D * forelder.C,
        D = barn.C * forelder.B + barn.D * forelder.D,
        E = barn.E * forelder.A + barn.F * forelder.C + forelder.E,
        F = barn.E * forelder.B + barn.F * forelder.D + forelder.F
    };

    private static void LesObjekt(IntPtr obj, FsMatrix forelder, Func<double, double, P> til, List<List<P>> linjer, List<Strek> streker, int dybde)
    {
        if (obj == IntPtr.Zero || dybde > 12) return;
        var type = FPDFPageObj_GetType(obj);
        if (type != ObjPath && type != ObjForm) return;
        FPDFPageObj_GetMatrix(obj, out var m);
        var mat = Gang(forelder, m);

        if (type == ObjForm)
        {
            var n = FPDFFormObj_CountObjects(obj);
            for (var i = 0; i < n; i++)
            {
                LesObjekt(FPDFFormObj_GetObject(obj, (uint)i), mat, til, linjer, streker, dybde + 1);
            }
            return;
        }

        P Punkt(float x, float y) => til(mat.A * x + mat.C * y + mat.E, mat.B * x + mat.D * y + mat.F);

        var segs = FPDFPath_CountSegments(obj);
        List<P>? naa = null;
        var bez = new List<P>();
        for (var i = 0; i < segs; i++)
        {
            var seg = FPDFPath_GetPathSegment(obj, i);
            if (FPDFPathSegment_GetPoint(seg, out var x, out var y) == 0) continue;
            var p = Punkt(x, y);
            var t = FPDFPathSegment_GetType(seg);
            if (t == SegMove || naa is null)
            {
                if (naa is { Count: > 1 }) linjer.Add(naa);
                naa = [p];
                bez.Clear();
                continue;
            }
            if (t == SegLine)
            {
                streker.Add(new Strek(naa[^1], p));
                naa.Add(p);
            }
            else if (t == SegBezier)
            {
                bez.Add(p);
                if (bez.Count == 3)
                {
                    // del kurven i biter så buer kan kjennes igjen
                    var p0 = naa[^1];
                    for (var k = 1; k <= 4; k++)
                    {
                        var u = k / 4.0;
                        var v = 1 - u;
                        naa.Add(p0 * (v * v * v) + bez[0] * (3 * v * v * u) + bez[1] * (3 * v * u * u) + bez[2] * (u * u * u));
                    }
                    bez.Clear();
                }
            }
        }
        if (naa is { Count: > 1 }) linjer.Add(naa);
    }

    // Ord satt sammen i tekstrekkefølge (fungerer også for rotert tekst).
    private static List<Ord> LesOrd(IntPtr side, Func<double, double, P> til)
    {
        var ut = new List<Ord>();
        var tekst = FPDFText_LoadPage(side);
        try
        {
            var n = FPDFText_CountChars(tekst);
            var sb = new StringBuilder();
            var punkter = new List<P>();
            double storrelse = 0;
            P? forrige = null;

            void Avslutt()
            {
                if (sb.Length > 0 && punkter.Count > 0)
                {
                    var midt = new P(punkter.Average(p => p.X), punkter.Average(p => p.Y));
                    ut.Add(new Ord(midt, storrelse, sb.ToString()));
                }
                sb.Clear();
                punkter.Clear();
                storrelse = 0;
                forrige = null;
            }

            for (var i = 0; i < n; i++)
            {
                var kode = FPDFText_GetUnicode(tekst, i);
                var c = kode <= 0xFFFF ? (char)kode : ' ';
                if (char.IsWhiteSpace(c) || c == '\0')
                {
                    Avslutt();
                    continue;
                }
                if (FPDFText_GetCharBox(tekst, i, out var l, out var r, out var b, out var t) == 0) continue;
                var p = til((l + r) / 2, (b + t) / 2);
                var str = Math.Max(Math.Max(r - l, t - b), FPDFText_GetFontSize(tekst, i) * 0.5);
                if (forrige is { } f && P.Avst(f, p) > Math.Max(str, storrelse) * 2.2)
                {
                    Avslutt();
                }
                sb.Append(c);
                punkter.Add(p);
                storrelse = Math.Max(storrelse, str);
                forrige = p;
            }
            Avslutt();
        }
        finally
        {
            FPDFText_ClosePage(tekst);
        }
        return ut;
    }
}
