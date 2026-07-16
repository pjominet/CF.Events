class ImageSizeTune {
    constructor({data, config, api}) {
        this.api = api;
        this.data = data || {
            size: '100' // Default to 100%
        };
    }

    static get isTune() {
        return true;
    }

    render() {
        const sizes = [
            {label: '25%', value: '25'},
            {label: '50%', value: '50'},
            {label: '75%', value: '75'},
            {label: '100%', value: '100'}
        ];

        return {
            icon: `<svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" fill="none" viewBox="-5 -5 24 24">
                     <path d="M0 3.5A1.5 1.5 0 0 1 1.5 2h13A1.5 1.5 0 0 1 16 3.5v9a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 0 12.5zM1.5 3a.5.5 0 0 0-.5.5v9a.5.5 0 0 0 .5.5h13a.5.5 0 0 0 .5-.5v-9a.5.5 0 0 0-.5-.5z"/>
                     <path d="M2 4.5a.5.5 0 0 1 .5-.5h3a.5.5 0 0 1 0 1H3v2.5a.5.5 0 0 1-1 0zm12 7a.5.5 0 0 1-.5.5h-3a.5.5 0 0 1 0-1H13V8.5a.5.5 0 0 1 1 0z"/>
                   </svg>`,
            title: 'Sizes',
            name: 'sizes',
            children: {
                items: sizes.map(size => ({
                    title: size.label,
                    icon: this.data.size === size.value ? `<svg width="20" height="20" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
                                                             <path d="M7.181 15.207l-4.148-4.148 1.415-1.415 2.733 2.733 6.131-6.131 1.415 1.415z"/>
                                                           </svg>` : '',
                    onActivate: () => {
                        this.selectSize(size.value);
                    }
                }))
            }
        };
    }

    selectSize(value) {
        this.data.size = value;

        // Apply style to the image in the editor for live preview
        const block = this.api.blocks.getCurrentBlockIndex();
        const blockElement = this.api.blocks.getBlockByIndex(block).holder;
        const img = blockElement.querySelector('img');
        if (img) {
            img.style.width = value + '%';
            img.style.height = 'auto';
        }

        // Close settings menu and update the UI
        this.api.toolbar.close();
    }

    save() {
        return this.data;
    }

    wrap(blockContent) {
        const img = blockContent.querySelector('img');
        if (img) {
            img.style.width = (this.data.size || '100') + '%';
            img.style.height = 'auto';
        }
        return blockContent;
    }
}
