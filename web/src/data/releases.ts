// Fuente única de versiones: web/public/releases.json.
// Ese archivo lo genera/actualiza el workflow changelog.yml al publicar un tag,
// y también lo sirve GitHub Pages en la raíz del sitio para que lo consuma la app.
// No edites las versiones acá: editá el JSON (o dejá que el CI lo haga).

import releasesData from '../../public/releases.json';

export interface Release {
  version: string;
  date: string;          // YYYY-MM-DD
  current?: boolean;
  installer: string;     // URL de descarga (GitHub Release)
  changes: { type: 'new' | 'fix' | 'change'; text: string }[];
}

export const releases: Release[] = (releasesData.releases as Release[]);

export const currentRelease = releases.find((r) => r.current) ?? releases[0];
