import { createApp, type Component } from 'vue';
import { createElevatorVuetify } from '@/plugins/vuetify';
import '@/styles/app.css';

export function mountApp(rootComponent: Component): void {
  const app = createApp(rootComponent);

  app.use(createElevatorVuetify());

  app.mount('#app');
}
