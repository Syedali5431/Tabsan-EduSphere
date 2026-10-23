# PDF.js (vendored)

- Package: `pdfjs-dist` **3.11.174** (Apache-2.0, see `LICENSE`), files `build/pdf.min.js` and `build/pdf.worker.min.js`.
- Used by the Plan M3 grading workspace and the student submission page (`wwwroot/js/submission-viewer.js`).
- Security: versions before 4.2.67 are affected by CVE-2024-4367 when `isEvalSupported` is on.
  The viewer always calls `getDocument({ ..., isEvalSupported: false })`, which is the documented mitigation.
  Keep that option if this file is replaced, or move to 4.2.67+ (ES-module build).
