/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** The API root including the version, e.g. https://localhost:7001/api/v1 or /api/v1. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
