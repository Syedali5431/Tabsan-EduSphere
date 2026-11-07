# Refreshes the contents page (and any other indexes) of a .docx with LibreOffice, then saves it as .docx and .pdf.
# For computers without Microsoft Word (finalize.ps1 drives Word). Run it with LibreOffice's own Python:
#   & "C:\Program Files\LibreOffice\program\python.exe" finalize-libreoffice.py <in.docx> <out.docx> <out.pdf>
import os
import subprocess
import sys
import time

import uno
from com.sun.star.beans import PropertyValue


def prop(name, value):
    p = PropertyValue()
    p.Name = name
    p.Value = value
    return p


def url(path):
    return uno.systemPathToFileUrl(os.path.abspath(path))


def main(src, out_docx, out_pdf, port=2083):
    office = os.path.join(os.path.dirname(sys.executable), 'soffice.exe')
    profile = url(os.path.join(os.environ.get('TEMP', '.'), 'edusphere-lo-profile'))  # own profile: no clash with an open LibreOffice
    proc = subprocess.Popen([office, '--headless', '--invisible', '--nologo', '--norestore', '--nodefault',
                             f'-env:UserInstallation={profile}', f'--accept=socket,host=127.0.0.1,port={port};urp;'])
    desktop = None
    try:
        local = uno.getComponentContext()
        resolver = local.ServiceManager.createInstanceWithContext('com.sun.star.bridge.UnoUrlResolver', local)
        ctx = None
        for _ in range(90):
            try:
                ctx = resolver.resolve(f'uno:socket,host=127.0.0.1,port={port};urp;StarOffice.ComponentContext')
                break
            except Exception:
                time.sleep(1)
        if ctx is None:
            raise RuntimeError('LibreOffice did not start')
        desktop = ctx.ServiceManager.createInstanceWithContext('com.sun.star.frame.Desktop', ctx)
        doc = desktop.loadComponentFromURL(url(src), '_blank', 0, (prop('Hidden', True),))
        # Twice: the first pass fills the contents page, which moves the chapters; the second gets their page numbers right.
        for _ in range(2):
            indexes = doc.getDocumentIndexes()
            for i in range(indexes.getCount()):
                indexes.getByIndex(i).update()
            doc.refresh()
        doc.storeToURL(url(out_docx), (prop('FilterName', 'MS Word 2007 XML'),))
        doc.storeToURL(url(out_pdf), (prop('FilterName', 'writer_pdf_Export'),))
        print('indexes updated:', doc.getDocumentIndexes().getCount(), '- pages:', doc.getCurrentController().PageCount)
        doc.close(True)
    finally:
        if desktop is not None:
            try:
                desktop.terminate()
            except Exception:
                pass
        try:
            proc.wait(timeout=60)
        except subprocess.TimeoutExpired:
            proc.kill()


if __name__ == '__main__':
    if len(sys.argv) != 4:
        sys.exit('usage: finalize-libreoffice.py <in.docx> <out.docx> <out.pdf>')
    main(*sys.argv[1:])
