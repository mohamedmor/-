(function () {
    var reduce = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // navbar gets a shadow once the page is scrolled
    var nav = document.querySelector('.site-header') || document.querySelector('.site-nav');
    if (nav) {
        var onScroll = function () { nav.classList.toggle('scrolled', window.scrollY > 8); };
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    // cards and category tiles appear one after another as they enter the screen
    var items = document.querySelectorAll('.reveal');
    if (items.length) {
        if (reduce || !('IntersectionObserver' in window)) {
            items.forEach(function (el) { el.classList.add('in'); });
        } else {
            var io = new IntersectionObserver(function (entries) {
                var i = 0;
                entries.forEach(function (e) {
                    if (!e.isIntersecting) return;
                    e.target.style.transitionDelay = (i++ * 70) + 'ms';
                    e.target.classList.add('in');
                    io.unobserve(e.target);
                });
            }, { threshold: 0.08, rootMargin: '0px 0px -30px 0px' });
            items.forEach(function (el) { io.observe(el); });
        }
    }

    // success messages fade away by themselves
    document.querySelectorAll('.alert-success').forEach(function (a) {
        setTimeout(function () {
            a.classList.add('leaving');
            setTimeout(function () { a.remove(); }, 600);
        }, 6000);
    });
})();
