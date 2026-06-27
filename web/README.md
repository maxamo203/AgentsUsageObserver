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

El instalador se distribuye por **GitHub Releases** (no se versiona en el repo).
El sitio en Pages se redeploya solo al pushear cambios en `web/`.

1. Subí la versión en `AgentUsageObserver.csproj` (`<Version>`) y en
   `installer.iss` (`#define AppVersion`).
2. Empujá un tag `vX.Y.Z`:
   ```bash
   git tag v0.2.0
   git push origin v0.2.0
   ```
   El workflow [`release.yml`](../.github/workflows/release.yml) compila la app,
   genera el instalador con Inno Setup y lo publica como Release automáticamente.
3. Agregá la nueva entrada arriba de todo en [`src/data/releases.ts`](src/data/releases.ts),
   marcala con `current: true`, quitale ese flag a la anterior y ajustá la URL
   del `installer` a la del nuevo tag.
4. Commiteá ese cambio en `web/` → Pages se redeploya con el nuevo botón.

Los botones de descarga del header, hero, versiones y CTA toman la URL
automáticamente desde `releases.ts`, así que no hay que tocar el resto.

> El primer deploy a Pages requiere activar **Settings → Pages → Source:
> GitHub Actions** una sola vez en el repo.

## Paleta

| Color    | Hex       |
|----------|-----------|
| Amarillo | `#FFC400` |
| Azul     | `#1591FF` |
| Verde    | `#B2DC1F` |
| Rojo     | `#FD2A1D` |
