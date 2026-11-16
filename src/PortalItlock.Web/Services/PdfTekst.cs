using System.Runtime.InteropServices;
using System.Text;

namespace PortalItlock.Web.Services;

// Leser ord med posisjon fra en PDF via PDFium - det samme native biblioteket som
// PDFtoImage bruker til å lage bilder av PDF-er (pdfium følger med den pakken).
// Koordinater er i punkter med origo øverst til venstre (y øker nedover).
public static class PdfTekst
{
    public sealed record Ord(int Side, double X0, double Y0, double X1, double Y1, string Tekst)
    {
        public double MidtY => (Y0 + Y1) / 2;
    }

    public sealed record Side(int Nr, double Bredde, double Hoyde, List<Ord> Ord);

    private static readonly object Laas = new();

    private const string Lib = "pdfium";

    [DllImport(Lib)] private static extern void FPDF_InitLibrary();
    [DllImport(Lib)] private static extern IntPtr FPDF_LoadMemDocument(IntPtr data, int size, string? password);
    [DllImport(Lib)] private static extern void FPDF_CloseDocument(IntPtr doc);
    [DllImport(Lib)] private static extern int FPDF_GetPageCount(IntPtr doc);
    [DllImport(Lib)] private static extern IntPtr FPDF_LoadPage(IntPtr doc, int index);
    [DllImport(Lib)] private static extern void FPDF_ClosePage(IntPtr page);
    [DllImport(Lib)] private static extern double FPDF_GetPageWidth(IntPtr page);
    [DllImport(Lib)] private static extern double FPDF_GetPageHeight(IntPtr page);
    [DllImport(Lib)] private static extern IntPtr FPDFText_LoadPage(IntPtr page);
    [DllImport(Lib)] private static extern void FPDFText_ClosePage(IntPtr textPage);
    [DllImport(Lib)] private static extern int FPDFText_CountChars(IntPtr textPage);
    [DllImport(Lib)] private static extern uint FPDFText_GetUnicode(IntPtr textPage, int index);
    [DllImport(Lib)] private static extern int FPDFText_GetCharBox(IntPtr textPage, int index, out double left, out double right, out double bottom, out double top);

    private static bool _init;

    public static List<Side> LesOrd(byte[] pdf, int maksSider = 20)
    {
        lock (Laas)
        {
            if (!_init)
            {
                FPDF_InitLibrary(); // PDFium ser selv om den allerede er startet
                _init = true;
            }

            var sider = new List<Side>();
            var handle = GCHandle.Alloc(pdf, GCHandleType.Pinned);
            var doc = IntPtr.Zero;
            try
            {
                doc = FPDF_LoadMemDocument(handle.AddrOfPinnedObject(), pdf.Length, null);
                if (doc == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Klarte ikke å åpne PDF-en.");
                }
                var antall = Math.Min(FPDF_GetPageCount(doc), maksSider);
                for (var s = 0; s < antall; s++)
                {
                    var side = FPDF_LoadPage(doc, s);
                    if (side == IntPtr.Zero)
                    {
                        continue;
                    }
                    try
                    {
                        var hoyde = FPDF_GetPageHeight(side);
                        var bredde = FPDF_GetPageWidth(side);
                        var tekst = FPDFText_LoadPage(side);
                        try
                        {
                            sider.Add(new Side(s + 1, bredde, hoyde, LagOrd(tekst, s + 1, hoyde)));
                        }
                        finally
                        {
                            FPDFText_ClosePage(tekst);
                        }
                    }
                    finally
                    {
                        FPDF_ClosePage(side);
                    }
                }
            }
            finally
            {
                if (doc != IntPtr.Zero)
                {
                    FPDF_CloseDocument(doc);
                }
                handle.Free();
            }
            return sider;
        }
    }

    private static List<Ord> LagOrd(IntPtr tekst, int sideNr, double sideHoyde)
    {
        var ord = new List<Ord>();
        var n = FPDFText_CountChars(tekst);
        var sb = new StringBuilder();
        double x0 = 0, y0 = 0, x1 = 0, y1 = 0;

        void Avslutt()
        {
            if (sb.Length > 0)
            {
                ord.Add(new Ord(sideNr, x0, y0, x1, y1, sb.ToString()));
                sb.Clear();
            }
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
            if (FPDFText_GetCharBox(tekst, i, out var l, out var r, out var b, out var t) == 0)
            {
                continue;
            }
            double cy0 = sideHoyde - t, cy1 = sideHoyde - b;
            if (sb.Length > 0)
            {
                var hoyde = Math.Max(1, Math.Max(y1 - y0, cy1 - cy0));
                var nyLinje = Math.Abs((cy0 + cy1) / 2 - (y0 + y1) / 2) > hoyde * 0.6;
                var gap = l - x1;
                if (nyLinje || gap > hoyde * 0.45 || gap < -hoyde * 2)
                {
                    Avslutt();
                }
            }
            if (sb.Length == 0)
            {
                x0 = l; y0 = cy0; x1 = r; y1 = cy1;
            }
            else
            {
                x0 = Math.Min(x0, l); x1 = Math.Max(x1, r);
                y0 = Math.Min(y0, cy0); y1 = Math.Max(y1, cy1);
            }
            sb.Append(c);
        }
        Avslutt();
        return ord;
    }
}
