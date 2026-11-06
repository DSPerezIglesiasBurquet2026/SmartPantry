import { RoutesService, eLayoutType } from '@abp/ng.core';
import { APP_INITIALIZER } from '@angular/core';

export const APP_ROUTE_PROVIDER = [
  { provide: APP_INITIALIZER, useFactory: configureRoutes, deps: [RoutesService], multi: true },
];

function configureRoutes(routes: RoutesService) {
  return () => {
    routes.add([
      {
        path: '/',
        name: '::Menu:Home',
        iconClass: 'fas fa-home',
        order: 1,
        layout: eLayoutType.application,
      },
      // 👇 Agrega este bloque para tu nueva pantalla
      {
        path: '/pantry', // Coincide con el path de app.routes.ts[cite: 9]
        name: '::Menu:Pantry', // Clave de localización (luego la traduciremos)[cite: 9]
        iconClass: 'fas fa-box',
        order: 2,
        layout: eLayoutType.application,
      }
    ]);
  };
}
