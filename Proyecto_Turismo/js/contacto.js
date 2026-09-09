const faqQuestions = document.querySelectorAll('.faq-question');

faqQuestions.forEach(question => {
    question.addEventListener('click', () => {
        const faqItem = question.parentElement;
        
        const currentlyActive = document.querySelector('.faq-item.active');
        if (currentlyActive && currentlyActive !== faqItem) {
            currentlyActive.classList.remove('active');
        }

        faqItem.classList.toggle('active');
    });
});