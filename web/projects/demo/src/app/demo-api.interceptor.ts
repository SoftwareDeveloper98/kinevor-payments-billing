import { HttpInterceptorFn } from '@angular/common/http';

export const demoApiInterceptor: HttpInterceptorFn = (request, next) => {
  if (!request.url.includes('/api/')) return next(request);

  return next(
    request.clone({
      setHeaders: {
        'X-Demo-Role': 'Staff',
        'X-Demo-Email': 'billing.demo@kinevor.local',
        'X-Demo-User': 'billing-demo-staff',
      },
    }),
  );
};
