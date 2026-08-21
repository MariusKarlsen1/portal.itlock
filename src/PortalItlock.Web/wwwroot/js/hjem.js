export function attachTilt(selector) {
    const cards = document.querySelectorAll(selector);

    cards.forEach(card => {
        if (card.dataset.tiltBound === '1') {
            return;
        }
        card.dataset.tiltBound = '1';

        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            return;
        }

        const maxTilt = 9;

        card.addEventListener('mousemove', (e) => {
            const rect = card.getBoundingClientRect();
            const x = (e.clientX - rect.left) / rect.width;
            const y = (e.clientY - rect.top) / rect.height;
            const rotateY = (x - 0.5) * 2 * maxTilt;
            const rotateX = (0.5 - y) * 2 * maxTilt;

            card.style.transform = `perspective(700px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) translateZ(4px)`;
            card.style.setProperty('--glare-x', `${x * 100}%`);
            card.style.setProperty('--glare-y', `${y * 100}%`);
        });

        card.addEventListener('mouseleave', () => {
            card.style.transform = '';
        });
    });
}
