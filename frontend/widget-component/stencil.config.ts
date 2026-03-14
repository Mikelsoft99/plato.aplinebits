import { Config } from '@stencil/core';

export const config: Config = {
  namespace: 'vacation-request',
  outputTargets: [
    {
      type: 'dist',
      esmLoaderPath: '../loader',
    },
    {
      type: 'www',
      serviceWorker: null,
    },
  ],
};
