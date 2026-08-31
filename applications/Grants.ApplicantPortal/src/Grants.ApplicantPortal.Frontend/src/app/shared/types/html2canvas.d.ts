// html2canvas 1.x ships no bundled TypeScript types and the published @types/html2canvas
// package targets the older 0.5 jQuery-plugin API, so we declare the modern function
// signature we actually use.
declare module 'html2canvas' {
  interface Html2CanvasOptions {
    scale?: number;
    useCORS?: boolean;
    backgroundColor?: string | null;
    logging?: boolean;
    /** Called with the cloned document/root element right before rasterization, letting callers mutate the clone. */
    onclone?: (document: Document, element: HTMLElement) => void;
  }

  export default function html2canvas(
    element: HTMLElement,
    options?: Html2CanvasOptions
  ): Promise<HTMLCanvasElement>;
}
