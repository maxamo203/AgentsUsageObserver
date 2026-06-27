// Construye una URL a un asset de /public respetando el `base` configurado
// (necesario en GitHub Pages, donde el sitio vive bajo un subpath).
export function asset(path: string): string {
  return `${import.meta.env.BASE_URL}/${path}`.replace(/\/{2,}/g, '/');
}
