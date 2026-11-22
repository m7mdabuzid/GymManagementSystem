// Confirm before deleting a member (or anything using .delete-link)
document.addEventListener("DOMContentLoaded", function () {
    const deletes = document.querySelectorAll(".delete-link");
    deletes.forEach(function (link) {
        link.addEventListener("click", function (e) {
            const ok = window.confirm("Are you sure you want to delete this item?");
            if (!ok) {
                e.preventDefault();
            }
        });
    });
});
// Simple count-up animation for dashboard numbers
document.addEventListener("DOMContentLoaded", function () {
    const counters = document.querySelectorAll(".counter");

    counters.forEach(counter => {
        const target = parseInt(counter.getAttribute("data-target")) || 0;
        let current = 0;

        if (target === 0) {
            counter.textContent = "0";
            return;
        }

        const duration = 600; // ms
        const step = Math.max(1, Math.floor(target / (duration / 16)));

        function update() {
            current += step;
            if (current >= target) {
                counter.textContent = target.toString();
            } else {
                counter.textContent = current.toString();
                requestAnimationFrame(update);
            }
        }

        update();
    });
});
