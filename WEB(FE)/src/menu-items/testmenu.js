// assets
import { IconBrandFramer, IconTypography, IconPalette, IconShadow, IconWindmill, IconLayoutGridAdd } from '@tabler/icons';

// constant
const icons = {
    IconTypography: IconTypography,
    IconPalette: IconPalette,
    IconShadow: IconShadow,
    IconWindmill: IconWindmill,
    IconBrandFramer: IconBrandFramer,
    IconLayoutGridAdd: IconLayoutGridAdd
};

//-----------------------|| UTILITIES MENU ITEMS ||-----------------------//

export const testmenu = {
    id: 'utilities',
    title: 'Event List',
    type: 'group',
    children: [
        {
            id: 'util-typography',
            title: 'Event List',
            type: 'item',
            url: '/views/pages/testlist',
            icon: icons['IconTypography'],
            breadcrumbs: false
        }
    ]
};
