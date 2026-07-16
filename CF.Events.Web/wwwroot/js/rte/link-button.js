class LinkButton {
    static get toolbox() {
        return {
            title: 'Link Button',
            icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" class="bi bi-caret-right-square" viewBox="-5 -5 24 24">
                     <path d="M14 1a1 1 0 0 1 1 1v12a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V2a1 1 0 0 1 1-1zM2 0a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V2a2 2 0 0 0-2-2z"/>
                     <path d="M5.795 12.456A.5.5 0 0 1 5.5 12V4a.5.5 0 0 1 .832-.374l4.5 4a.5.5 0 0 1 0 .748l-4.5 4a.5.5 0 0 1-.537.082"/>
                   </svg>`
        };
    }

    constructor({data, config, api}) {
        this.api = api;
        this.data = {
            text: data.text || '',
            route: data.route || '',
            type: data.type || 'primary',
            outline: data.outline || false
        };
        this.nodes = {
            wrapper: null,
            textInput: null,
            routeInput: null,
            typeSelect: null,
            outlineCheck: null
        };
    }

    render() {
        const wrapper = document.createElement('div');
        wrapper.classList.add('link-button-tool', 'ce-block--focused');
        wrapper.style.padding = '15px';
        wrapper.style.backgroundColor = '#f9f9f9';
        wrapper.style.border = '1px solid #e8e8eb';
        wrapper.style.borderRadius = '6px';

        const createInput = (label, value, placeholder) => {
            const container = document.createElement('div');
            container.style.marginBottom = '10px';

            const l = document.createElement('label');
            l.textContent = label;
            l.style.display = 'block';
            l.style.fontSize = '11px';
            l.style.fontWeight = '700';
            l.style.textTransform = 'uppercase';
            l.style.color = '#707684';
            l.style.marginBottom = '4px';

            const input = document.createElement('input');
            input.classList.add('cdx-input');
            input.style.width = '100%';
            input.style.padding = '8px 12px';
            input.value = value;
            input.placeholder = placeholder;

            container.appendChild(l);
            container.appendChild(input);
            return {container, input};
        };

        const textRes = createInput('Button Text', this.data.text, 'Enter button text...');
        this.nodes.textInput = textRes.input;

        const routeRes = createInput('Route (use {id} for params)', this.data.route, '/preview/{id}');
        this.nodes.routeInput = routeRes.input;

        const typeContainer = document.createElement('div');
        typeContainer.style.marginBottom = '10px';
        const labelType = document.createElement('label');
        labelType.textContent = 'Button Type';
        labelType.style.display = 'block';
        labelType.style.fontSize = '11px';
        labelType.style.fontWeight = '700';
        labelType.style.textTransform = 'uppercase';
        labelType.style.color = '#707684';
        labelType.style.marginBottom = '4px';

        this.nodes.typeSelect = document.createElement('select');
        this.nodes.typeSelect.classList.add('cdx-input');
        this.nodes.typeSelect.style.width = '100%';
        this.nodes.typeSelect.style.padding = '8px 12px';
        this.nodes.typeSelect.style.appearance = 'auto'; // Show dropdown arrow
        ['primary', 'secondary', 'success', 'danger', 'warning', 'info', 'light', 'dark'].forEach(type => {
            const option = document.createElement('option');
            option.value = type;
            option.textContent = type.charAt(0).toUpperCase() + type.slice(1);
            option.selected = this.data.type === type;
            this.nodes.typeSelect.appendChild(option);
        });
        typeContainer.appendChild(labelType);
        typeContainer.appendChild(this.nodes.typeSelect);

        const outlineWrapper = document.createElement('div');
        outlineWrapper.style.display = 'flex';
        outlineWrapper.style.alignItems = 'center';
        outlineWrapper.style.marginTop = '10px';

        this.nodes.outlineCheck = document.createElement('input');
        this.nodes.outlineCheck.type = 'checkbox';
        this.nodes.outlineCheck.style.marginRight = '8px';
        this.nodes.outlineCheck.id = 'outlineCheck-' + Math.random().toString(36).substr(2, 9);
        this.nodes.outlineCheck.checked = this.data.outline;

        const labelOutline = document.createElement('label');
        labelOutline.style.fontSize = '13px';
        labelOutline.style.cursor = 'pointer';
        labelOutline.htmlFor = this.nodes.outlineCheck.id;
        labelOutline.textContent = 'Use Outline Style';

        outlineWrapper.appendChild(this.nodes.outlineCheck);
        outlineWrapper.appendChild(labelOutline);

        wrapper.appendChild(textRes.container);
        wrapper.appendChild(routeRes.container);
        wrapper.appendChild(typeContainer);
        wrapper.appendChild(outlineWrapper);

        this.nodes.wrapper = wrapper;
        return wrapper;
    }

    save(blockContent) {
        return {
            text: this.nodes.textInput.value,
            route: this.nodes.routeInput.value,
            type: this.nodes.typeSelect.value,
            outline: this.nodes.outlineCheck.checked
        };
    }
}
