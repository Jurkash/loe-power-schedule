(function () {
    window.addEventListener("load", function () {
        setTimeout(function () {
            let head = document.getElementsByTagName('head')[0];
            let icons = head.querySelectorAll("link[rel='icon']");
            for (let i = 0; i < icons.length; i++) {
                console.log(icons[i]);
                head.removeChild(icons[i]);
            }
            let icon = document.createElement('link');
            icon.setAttribute('rel','icon');
            icon.setAttribute('href','../favicon.svg');
            icon.setAttribute('type','image/svg+xml');
            head.appendChild(icon);

            let logoLink = document.getElementsByClassName('link')[0];
            logoLink.children[0].src = '/swagger-ui/bulb.svg';
            let logoText = document.createElement('span');
            logoText.innerText = 'LOE API';
            logoLink.appendChild(logoText);

            let wrapper = document.getElementsByClassName('topbar-wrapper')[0];

            let selector = document.getElementsByClassName('download-url-wrapper')[0];

            let node = document.createElement('div');
            node.setAttribute('id', 'j-bmc-btn');

            let link = document.createElement('a');
            link.setAttribute('href', 'https://www.buymeacoffee.com/jurkash')

            // let img = document.createElement('img');
            // img.setAttribute('src','/swagger-ui/bmc-img.svg');
            // img.setAttribute('width','185px');
            // img.setAttribute('height','50px');

            // link.appendChild(img);
            node.appendChild(link);

            wrapper.replaceChild(node, selector);

            // Fetch the SVG content from the URL
            let xhr = new XMLHttpRequest();
            xhr.open('GET', '/swagger-ui/bmc-img.svg', true);
            xhr.onload = function () {
                if (xhr.status >= 200 && xhr.status < 300) {
                    let svgContent = xhr.responseText;
                    let svg = document.createElement('svg');
                    svg.innerHTML = svgContent;

                    svg.setAttribute('width', '185px');
                    svg.setAttribute('height', '50px');
                    link.appendChild(svg);
                    node.appendChild(link);
                } else {
                    console.error('Failed to load SVG');
                }
            };
            xhr.send();
        });
    });
})();
