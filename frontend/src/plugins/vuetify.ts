import '@mdi/font/css/materialdesignicons.css';
import 'vuetify/styles';
import { createVuetify } from 'vuetify';
import { mdi } from 'vuetify/iconsets/mdi';

export function createElevatorVuetify() {
  return createVuetify({
    icons: {
      defaultSet: 'mdi',
      sets: {
        mdi
      }
    },
    theme: {
      defaultTheme: 'elevatorTheme',
      themes: {
        elevatorTheme: {
          dark: false,
          colors: {
            primary: '#2563EB',
            secondary: '#0F172A',
            accent: '#14B8A6',
            background: '#F6F8FC',
            surface: '#FFFFFF',
            success: '#16A34A',
            warning: '#F59E0B',
            error: '#DC2626',
            info: '#0891B2'
          }
        }
      }
    },
    defaults: {
      VBtn: {
        rounded: 'lg'
      },
      VCard: {
        rounded: 'xl',
        elevation: 2
      },
      VTextField: {
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto'
      },
      VTextarea: {
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto'
      },
      VSelect: {
        variant: 'outlined',
        density: 'comfortable',
        hideDetails: 'auto'
      },
      VDialog: {
        maxWidth: 680
      }
    }
  });
}
