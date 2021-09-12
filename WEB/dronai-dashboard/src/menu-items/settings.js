// assets
import { IconWaveSawTool, IconHelp, IconSitemap } from '@tabler/icons';

// constant
const icons = {
    IconWaveSawTool: IconWaveSawTool,
    IconHelp: IconHelp,
    IconSitemap: IconSitemap
};

//-----------------------|| SETTING PAGE ||-----------------------//

export const settings = {
    id: 'settings',
    title: 'Settings',
    type: 'group',
    children: [
        {
            id: 'connection',
            title: 'Connection',
            type: 'item',
            url: '/settings/connection',
            icon: icons['IconWaveSawTool'],
            breadcrumbs: false
        }
    ]
};
