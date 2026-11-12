from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

ROOT = Path(r"D:\CorexProd_v1")
OUT = ROOT / "manual_stock_productos.docx"
IMG = ROOT / "manual_stock_imagenes"
IMG.mkdir(exist_ok=True)

FONT = r"C:\Windows\Fonts\segoeui.ttf"
FONT_B = r"C:\Windows\Fonts\segoeuib.ttf"

def ft(size, bold=False):
    try:
        return ImageFont.truetype(FONT_B if bold else FONT, size)
    except Exception:
        return ImageFont.load_default()

def screenshot_login(path):
    im = Image.new("RGB", (1365, 768), "#F1F5F9")
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, 1365, 44), fill="#0F172A")
    d.text((22, 11), "CorexProd", font=ft(19, True), fill="white")
    d.rounded_rectangle((455, 125, 910, 630), radius=12, fill="white", outline="#E2E8F0", width=2)
    d.text((520, 175), "CorexProd", font=ft(30, True), fill="#0F172A")
    d.text((520, 220), "Inicio de sesión", font=ft(20, True), fill="#334155")
    d.text((520, 282), "Usuario", font=ft(14, True), fill="#334155")
    d.rounded_rectangle((520, 307, 845, 348), radius=5, fill="white", outline="#CBD5E1", width=2)
    d.text((535, 317), "Ingrese su usuario", font=ft(14), fill="#94A3B8")
    d.text((520, 378), "Contraseña", font=ft(14, True), fill="#334155")
    d.rounded_rectangle((520, 403, 845, 444), radius=5, fill="white", outline="#CBD5E1", width=2)
    d.text((535, 413), "Ingrese su contraseña", font=ft(14), fill="#94A3B8")
    d.rounded_rectangle((520, 482, 845, 526), radius=5, fill="#2563EB")
    d.text((653, 494), "Ingresar", font=ft(15, True), fill="white")
    d.text((492, 670), "Figura 1. Pantalla de acceso al sistema", font=ft(14), fill="#475569")
    im.save(path)

def screenshot_stock(path):
    W, H = 1500, 820
    im = Image.new("RGB", (W, H), "#F8FAFC")
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, W, 60), fill="#0F172A")
    d.text((26, 17), "CorexProd", font=ft(22, True), fill="white")
    d.text((1300, 18), "Usuario", font=ft(15), fill="#CBD5E1")
    d.rectangle((0, 60, 255, H), fill="#111827")
    d.text((25, 92), "MENÚ PRINCIPAL", font=ft(12, True), fill="#94A3B8")
    d.text((32, 145), "Inicio", font=ft(16), fill="#CBD5E1")
    d.text((32, 201), "Productos", font=ft(16), fill="#CBD5E1")
    d.text((32, 257), "Ventas", font=ft(16), fill="#CBD5E1")
    d.rounded_rectangle((14, 302, 241, 350), radius=7, fill="#2563EB")
    d.text((32, 316), "Reportes", font=ft(16, True), fill="white")
    d.text((50, 372), "• Stock Productos", font=ft(15), fill="#93C5FD")
    d.text((50, 414), "• Stock Reservas", font=ft(15), fill="#CBD5E1")
    d.text((50, 456), "• Kardex Productos", font=ft(15), fill="#CBD5E1")
    x0, y0 = 285, 88
    d.text((x0, y0), "Stock de Productos", font=ft(29, True), fill="#0F172A")
    d.text((x0, y0+42), "Consulta de código, producto, etiqueta, stock físico, reservado y disponible real.", font=ft(15), fill="#64748B")
    d.rounded_rectangle((1110, 90, 1215, 155), radius=7, fill="#EFF6FF", outline="#BFDBFE", width=2)
    d.text((1127, 103), "Productos", font=ft(12), fill="#1D4ED8")
    d.text((1152, 125), "128", font=ft(20, True), fill="#1E3A8A")
    d.rounded_rectangle((1230, 90, 1415, 155), radius=7, fill="#ECFDF5", outline="#A7F3D0", width=2)
    d.text((1245, 103), "Disponible total", font=ft(12), fill="#047857")
    d.text((1300, 125), "4,520.00", font=ft(20, True), fill="#065F46")
    d.rounded_rectangle((285, 185, 1415, 320), radius=9, fill="white", outline="#E5E7EB", width=2)
    labels = [(310, "Buscar por código o nombre", "PRD-001"), (610, "Etiqueta / Cliente", "Cliente A"), (850, "Categoría", "Todas las categorías")]
    for x, lab, val in labels:
        d.text((x, 205), lab, font=ft(13, True), fill="#334155")
        d.rounded_rectangle((x, 232, x+220, 270), radius=5, fill="white", outline="#CBD5E1", width=2)
        d.text((x+10, 242), val, font=ft(13), fill="#475569")
    for x, label, color in [(1100, "Buscar", "#2563EB"), (1200, "Limpiar", "#64748B"), (1300, "Actualizar", "#0F766E")]:
        d.rounded_rectangle((x, 232, x+95, 270), radius=5, fill=color)
        d.text((x+15, 243), label, font=ft(12, True), fill="white")
    cols = [(300, "CÓDIGO", 100), (400, "PRODUCTO", 390), (790, "STOCK FÍSICO", 125), (915, "RESERVA", 110), (1025, "DISPONIBLE", 120), (1145, "ETIQUETA / CLIENTE", 165), (1310, "CATEGORÍA", 170)]
    top = 355
    d.rectangle((285, top, 1415, top+45), fill="#DDE6F0", outline="#CBD5E1")
    for x, label, width in cols:
        d.text((x+7, top+14), label, font=ft(11, True), fill="#1F2937")
    rows = [("PRD-001", "Producto de ejemplo", "120.00", "20.00", "100.00", "Cliente A", "General"), ("PRD-002", "Producto de ejemplo 2", "85.00", "0.00", "85.00", "-", "General"), ("PRD-003", "Producto de ejemplo 3", "12.00", "12.00", "0.00", "Cliente B", "Especial")]
    for i, row in enumerate(rows):
        y = top+45+i*53
        d.rectangle((285, y, 1415, y+53), fill="white" if i%2==0 else "#F8FAFC", outline="#E2E8F0")
        xpos = [300, 400, 790, 915, 1025, 1145, 1310]
        for x, val in zip(xpos, row):
            d.text((x+7, y+18), val, font=ft(12), fill="#0F172A")
    d.text((520, 760), "Figura 2. Pantalla Stock de Productos con filtros y columnas de disponibilidad", font=ft(14), fill="#475569")
    im.save(path)

def shade(cell, fill):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement('w:shd'); shd.set(qn('w:fill'), fill); tcPr.append(shd)

def set_cell_text(cell, text, bold=False, color="000000", size=9):
    cell.text = ""
    p = cell.paragraphs[0]
    r = p.add_run(text); r.bold = bold; r.font.size = Pt(size); r.font.color.rgb = RGBColor.from_string(color)
    cell.vertical_alignment = WD_CELL_VERTICAL_ALIGNMENT.CENTER

def add_bullets(doc, items):
    for item in items:
        p = doc.add_paragraph(style="List Bullet")
        p.paragraph_format.space_after = Pt(3)
        p.add_run(item)

def add_caption(doc, text):
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r = p.add_run(text); r.italic = True; r.font.size = Pt(9); r.font.color.rgb = RGBColor(71,85,105)

def main():
    login = IMG / "pantalla_login.png"; stock = IMG / "pantalla_stock.png"
    screenshot_login(login); screenshot_stock(stock)
    doc = Document()
    sec = doc.sections[0]
    sec.top_margin = Inches(.65); sec.bottom_margin = Inches(.65); sec.left_margin = Inches(.7); sec.right_margin = Inches(.7)
    styles = doc.styles
    styles['Normal'].font.name = 'Aptos'; styles['Normal'].font.size = Pt(10); styles['Normal']._element.rPr.rFonts.set(qn('w:eastAsia'), 'Aptos')
    for name, size in [('Title', 25), ('Heading 1', 17), ('Heading 2', 12)]:
        styles[name].font.name = 'Aptos Display'; styles[name].font.size = Pt(size); styles[name].font.color.rgb = RGBColor(15,23,42)
    p = doc.add_paragraph(style='Title'); p.alignment = WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Manual para revisar el stock de productos')
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r=p.add_run('Sistema CorexProd'); r.font.size=Pt(13); r.font.color.rgb=RGBColor(37,99,235)
    p = doc.add_paragraph(); p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    r=p.add_run('Guía rápida para consultar stock físico, reservado y disponible'); r.font.size=Pt(11); r.font.color.rgb=RGBColor(71,85,105)
    doc.add_picture(str(stock), width=Inches(6.9)); add_caption(doc, 'Vista de referencia de la consulta de stock. Los valores mostrados son ilustrativos.')
    doc.add_heading('1. Objetivo', level=1)
    doc.add_paragraph('Este manual explica cómo consultar el stock de productos en CorexProd, localizar un producto y distinguir entre la cantidad física, la cantidad reservada y la cantidad disponible para nuevas operaciones.')
    doc.add_heading('2. Ingreso al sistema', level=1)
    doc.add_paragraph('Abra CorexProd e ingrese con el usuario y la contraseña asignados. No comparta sus credenciales con otras personas.')
    doc.add_picture(str(login), width=Inches(5.9)); add_caption(doc, 'Figura 1. Pantalla de inicio de sesión del sistema.')
    doc.add_heading('3. Abrir la consulta de stock', level=1)
    doc.add_paragraph('En el menú lateral, seleccione Reportes y luego Stock Productos. El sistema mostrará el listado de productos y un resumen con el total de productos y el disponible total.')
    doc.add_heading('4. Buscar un producto', level=1)
    doc.add_paragraph('Use uno o varios filtros y luego presione Buscar. Los filtros se pueden combinar para reducir los resultados.')
    add_bullets(doc, [
        'Buscar por código o nombre: escriba el código interno o parte del nombre del producto.',
        'Etiqueta / Cliente: escriba la etiqueta o el cliente asociado al producto.',
        'Categoría: seleccione una categoría o deje Todas las categorías.',
        'Limpiar: elimina los filtros ingresados y muestra nuevamente el listado completo.',
        'Actualizar: vuelve a cargar la información desde el sistema.'
    ])
    doc.add_heading('5. Interpretar los resultados', level=1)
    table = doc.add_table(rows=1, cols=3); table.alignment = WD_TABLE_ALIGNMENT.CENTER; table.style = 'Table Grid'
    for i, h in enumerate(['Campo', 'Qué significa', 'Qué revisar']):
        set_cell_text(table.rows[0].cells[i], h, True, 'FFFFFF', 9); shade(table.rows[0].cells[i], '1D4ED8')
    data = [
        ('Stock físico', 'Cantidad registrada físicamente en el almacén.', 'Comparar con el conteo físico cuando sea necesario.'),
        ('Reserva', 'Cantidad comprometida o separada para un proceso.', 'Si tiene valor, no está libre para nuevas operaciones.'),
        ('Disponible', 'Cantidad que queda disponible para utilizar.', 'Es la cifra principal para confirmar disponibilidad.'),
        ('Almacén', 'Ubicación donde se encuentra el stock.', 'Verificar que corresponda al almacén esperado.')]
    for row in data:
        cells = table.add_row().cells
        for i, text in enumerate(row): set_cell_text(cells[i], text, False, '0F172A', 9)
    doc.add_paragraph('Como regla práctica: Disponible = Stock físico - Reserva. Si el disponible aparece en cero, el producto no tiene cantidad libre aunque exista stock físico.')
    doc.add_heading('6. Consultar reservas', level=1)
    doc.add_paragraph('Cuando la columna Reserva muestra una cantidad mayor que cero, el valor se presenta como un enlace. Haga clic sobre la cantidad para abrir la consulta de Stock Reservas en Proceso filtrada por ese producto.')
    add_bullets(doc, ['Revise el producto y el código antes de interpretar la reserva.', 'Use Actualizar si el resultado no refleja un movimiento reciente.', 'Si observa una diferencia entre stock físico y disponible, verifique las reservas activas y el almacén.'])
    doc.add_heading('7. Recomendaciones', level=1)
    add_bullets(doc, ['Realice la consulta antes de confirmar una venta, despacho o ingreso a producción.', 'Use el código del producto cuando existan nombres parecidos.', 'No confunda stock físico con disponible.', 'Si el stock no coincide con el conteo, reporte la diferencia al responsable de almacén para su revisión.', 'No modifique información desde esta pantalla: es una consulta de stock.'])
    doc.add_heading('8. Solución de problemas frecuentes', level=1)
    table = doc.add_table(rows=1, cols=2); table.style='Table Grid'; table.alignment=WD_TABLE_ALIGNMENT.CENTER
    for i,h in enumerate(['Situación', 'Acción recomendada']):
        set_cell_text(table.rows[0].cells[i], h, True, 'FFFFFF', 9); shade(table.rows[0].cells[i], '0F766E')
    probs=[('No aparece el producto', 'Limpie filtros, busque por código y presione Actualizar.'), ('Disponible es menor que el físico', 'Revise la columna Reserva y consulte Stock Reservas en Proceso.'), ('No se muestran datos', 'Verifique permisos, conexión y que el usuario tenga acceso al reporte.'), ('La información parece antigua', 'Presione Actualizar y vuelva a realizar la búsqueda.')]
    for a,b in probs:
        cells=table.add_row().cells; set_cell_text(cells[0],a,False,'0F172A',9); set_cell_text(cells[1],b,False,'0F172A',9)
    doc.add_paragraph()
    p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('Fin del manual'); r.bold=True; r.font.color.rgb=RGBColor(37,99,235)
    footer=sec.footer.paragraphs[0]; footer.alignment=WD_ALIGN_PARAGRAPH.CENTER; footer.add_run('CorexProd | Manual de consulta de stock').font.size=Pt(8)
    doc.save(OUT)
    print(OUT)

if __name__ == '__main__': main()
