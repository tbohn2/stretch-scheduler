import { privateServices } from './root.js';

let mobile = window.innerWidth < 768 ? true : false;

function renderServices() {
    const servicesContainer = $('#services');
    servicesContainer.empty();

    const serviceCards = privateServices.map(service => {
        let card;
        const imgURL = '.' + service.ImgURL.slice(5);
        mobile ?
            card = `
                <div class="serviceCard league my-3 d-flex fade-top">
                    <img class="col-2 col-xl-1" src="${imgURL}" alt="yoga">
                    <div class="py-1 col-10 col-xl-11 position-relative d-flex flex-column justify-content-evenly">
                        <div class="px-2 d-flex align-items-center justify-content-between">
                            <h3 class="m-0">${service.Name}</h3>
                            <p class="m-0">$${service.Price} | ${service.Duration} min</p>                        
                        </div>
                        <div class="col-12 d-flex px-2">
                            <p id=${service.Id + 'desc'} class="serviceDescription col-10 fs-5">${service.ShortDescription}</p>
                            <p id=${service.Id + 'book'} class="serviceCard-button text-center col-2 fs-5">Book</p>                        
                        </div>
                        <p id=${service.Id + 'descDisplay'} class="displayDescription col-10 ms-4 fs-4 align-self-start">${service.Description}</p>
                    </div>
                </div>`
            :
            card = `
                <div class="serviceCard league my-3 d-flex flex-column align-items-center justify-content-between fade-top">
                    <img class="col-12" src="${imgURL}" alt="yoga">
                    <div class="col-12 position-relative d-flex flex-column align-items-center justify-content-start">
                        <h3 class="mt-3 align-self-center text-center">${service.Name}</h3>
                        <p class="m-0 text-center">$${service.Price} | ${service.Duration} min</p>
                        <p id=${service.Id + 'desc'} class="serviceDescription text-center col-12 mb-0">${service.ShortDescription}</p>
                        <p id=${service.Id + 'descDisplay'} class="displayDescription col-10 fs-4 align-self-start">${service.Description}</p>
                    </div>
                    <p id=${service.Id + 'book'} class="serviceCard-button text-center fw-bold fs-5">Book</p>                        
                </div>`;
        return card;
    }).join(''); // Join all cards into a single string

    servicesContainer.append(serviceCards); // Append all cards at once

    // Add event listeners to all buttons
    $('.serviceCard-button').on('click', function () {
        const serviceId = $(this).attr('id').slice(0, -4);
        localStorage.setItem('bookServiceId', serviceId);
        window.location.assign('./calendar');
    });

    $('.serviceDescription').on("mouseenter",
        function (event) {
            let id = event.target.id;
            $('#' + id + 'Display').addClass('show');
            $('#overlay').addClass('show');
            event.stopPropagation();
        },
    )
    $('#overlay').on("mouseenter",
        function (event) {
            $('.displayDescription.show').removeClass('show');
            $('.displayRollModel.show').removeClass('show');
            $('#overlay').removeClass('show');
            event.stopPropagation();
        }
    );

    $('.serviceDescription').on('click', function (event) {
        $(this).addClass('text-decoration-underline');
        let id = event.target.id;
        $('#' + id + 'Display').addClass('show');
        $('#overlay').addClass('show');
        event.stopPropagation();
    });

    $('.displayDescription').on('click', function (event) {
        let id = event.target.id;
        $('#' + id).removeClass('show');
        $('#overlay').removeClass('show');
        $('.serviceDescription.text-decoration-underline').removeClass('text-decoration-underline');
        event.stopPropagation();
    }
    );
};

$('#overlay').on('click', function () {
    $('.displayDescription.show').removeClass('show');
    $('.displayRollModel.show').removeClass('show');
    $('#overlay').removeClass('show');
    $('.serviceDescription.text-decoration-underline').removeClass('text-decoration-underline');
});

renderServices();

$('#calendar-link').on('click', function () {
    window.location.assign('./calendar');
});

window.addEventListener('resize', () => {
    let isMobile = window.innerWidth < 768 ? true : false;
    if (isMobile !== mobile) {
        mobile = !mobile;
        renderServices();
    }
});