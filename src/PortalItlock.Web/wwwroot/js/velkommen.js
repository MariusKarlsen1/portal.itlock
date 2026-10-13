// Animasjonene paa landingssiden (/velkommen). Ren JS, ingen bibliotek.
//
// Tre ting:
//   1. .vk-rull - seksjoner glir inn naar de kommer i synsfeltet
//   2. .vk-mock-tall - teller opp til data-tell naar hero kommer til syne
//   3. .vk-nav - faar skygge og bakgrunn naar man har rullet litt
//
// Respekterer prefers-reduced-motion: da vises alt ferdig med en gang.
(function () {
    const roligBevegelse = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function visAlt() {
        document.querySelectorAll('.vk-rull').forEach(function (el) {
            el.classList.add('vk-synlig');
        });
        document.querySelectorAll('.vk-mock-tall').forEach(function (el) {
            el.textContent = el.dataset.tell || el.textContent;
        });
    }

    if (roligBevegelse || typeof IntersectionObserver !== 'function') {
        visAlt();
        return;
    }

    // ---------- Innrulling ----------
    const vakt = new IntersectionObserver(function (poster) {
        poster.forEach(function (post) {
            if (!post.isIntersecting) {
                return;
            }

            post.target.classList.add('vk-synlig');
            // Hver seksjon rulles inn EN gang - den skal ikke blinke igjen
            // naar man ruller opp og ned.
            vakt.unobserve(post.target);
        });
    }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

    document.querySelectorAll('.vk-rull').forEach(function (el) {
        vakt.observe(el);
    });

    // Sikkerhetsnett: .vk-rull starter usynlig, saa hvis observatoeren aldri
    // utloeses - nullstort vindu, skjult fane, en nettleser som oppfoerer seg
    // uventet - ville halve siden blitt staaende tom. Har ingenting rullet inn
    // etter to sekunder, vises alt. Innhold skal aldri kunne bli usynlig for
    // godt fordi en animasjon ikke kom i gang.
    window.setTimeout(function () {
        if (document.querySelectorAll('.vk-synlig').length === 0) {
            visAlt();
        }
    }, 2000);

    // ---------- Telle opp noekkeltallene i hero-figuren ----------
    function tellOpp(el) {
        const mal = parseInt(el.dataset.tell || '0', 10);
        if (!mal) {
            return;
        }

        const varighet = 900;
        const start = performance.now();

        function steg(na) {
            const andel = Math.min((na - start) / varighet, 1);
            // Bremser mot slutten, slik at tallet lander mykt.
            const lettet = 1 - Math.pow(1 - andel, 3);
            el.textContent = Math.round(mal * lettet);

            if (andel < 1) {
                requestAnimationFrame(steg);
            }
        }

        requestAnimationFrame(steg);
    }

    const tallvakt = new IntersectionObserver(function (poster) {
        poster.forEach(function (post) {
            if (!post.isIntersecting) {
                return;
            }

            tellOpp(post.target);
            tallvakt.unobserve(post.target);
        });
    }, { threshold: 0.5 });

    document.querySelectorAll('.vk-mock-tall').forEach(function (el) {
        tallvakt.observe(el);
    });

    // ---------- Toppfeltet ----------
    const nav = document.getElementById('vk-nav');
    if (nav) {
        let sistRullet = -1;

        const oppdaterNav = function () {
            const rullet = window.scrollY > 24;
            if (rullet !== sistRullet) {
                sistRullet = rullet;
                nav.classList.toggle('vk-nav-rullet', rullet);
            }
        };

        oppdaterNav();
        window.addEventListener('scroll', oppdaterNav, { passive: true });
    }
})();
