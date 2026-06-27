# Sitio web — Agent Usage Observer

Landing en Astro para promocionar la app y servir el instalador.

## Desarrollo

```bash
cd web
npm install
npm run dev      # http://localhost:4321
```

## Build

```bash
npm run build    # genera el sitio estático en web/dist/
npm run preview  # previsualiza el build
```

## Cómo publicar una nueva versión

1. Compilá el instalador con Inno Setup (`installer.iss` en la raíz del repo).
2. Copiá el `.exe` resultante a `web/public/downloads/`.
3. Agregá la nueva entrada arriba de todo en [`src/data/releases.ts`](src/data/releases.ts),
   marcala con `current: true` y quitale ese flag a la anterior.
4. Asegurate de que el campo `installer` apunte al nombre del archivo copiado.

Los botones de descarga del header, hero, versiones y CTA toman la ruta
automáticamente desde `releases.ts`, así que no hay que tocar el resto.

## Paleta

| Color    | Hex       |
|----------|-----------|
| Amarillo | `#FFC400` |
| Azul     | `#1591FF` |
| Verde    | `#B2DC1F` |
| Rojo     | `#FD2A1D` |
