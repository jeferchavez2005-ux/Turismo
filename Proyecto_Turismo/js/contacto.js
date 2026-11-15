const faqQuestions = document.querySelectorAll(".faq-q, .faq-question");

faqQuestions.forEach(question => {
    question.addEventListener('click', () => {
        const faqItem = question.parentElement;
        
        const currentlyActive = document.querySelector('.faq-item.active');
        if (currentlyActive && currentlyActive !== faqItem) {
            currentlyActive.classList.remove('active');
        }

        const expanded = faqItem.classList.toggle("active");
        question.setAttribute("aria-expanded", String(expanded));
    });
});
