// Datos de las versiones. Agregá una nueva entrada arriba al lanzar una versión.
// `current: true` marca la versión que descarga el botón principal.

export interface Release {
  version: string;
  date: string;          // YYYY-MM-DD
  current?: boolean;
  installer: string;     // URL de descarga (GitHub Release)
  changes: { type: 'new' | 'fix' | 'change'; text: string }[];
}

// URL base de los assets publicados en GitHub Releases.
const REL = 'https://github.com/maxamo203/AgentsUsageObserver/releases/download';

export const releases: Release[] = [
  {
    version: '0.1.0',
    date: '2026-06-26',
    current: true,
    installer: `${REL}/v0.1.0/AgentUsageObserver-0.1.0-Setup.exe`,
    changes: [
      { type: 'new', text: 'Monitor de uso de agentes de IA desde la bandeja del sistema (empezando por Claude Code).' },
      { type: 'new', text: 'Botón de refresh junto al nombre de cada agente para actualizar solo su información.' },
      { type: 'new', text: 'Icono de color según el consumo (verde / amarillo / rojo).' },
      { type: 'new', text: 'Barras de límite de 5 horas y semanal con cuenta regresiva de reinicio.' },
      { type: 'new', text: 'Umbrales de color configurables o nivel de aviso reportado por el servidor.' },
      { type: 'new', text: 'Frecuencia de actualización ajustable.' },
      { type: 'new', text: 'Opción de iniciar con Windows.' },
      { type: 'new', text: 'Soporte multi-idioma (inglés, español, portugués y más).' },
    ],
  },
];

export const currentRelease = releases.find((r) => r.current) ?? releases[0];
