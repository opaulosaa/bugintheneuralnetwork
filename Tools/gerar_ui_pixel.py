#!/usr/bin/env python3
# Gera os sprites pixel da interface (menu e caixas dos puzzles) em Assets/UI/Pixel.
# Uso (na pasta do projeto): python3 Tools/gerar_ui_pixel.py   | Requer Pillow.
# Os sprites são desenhados em tamanho real (1 px) e a UI amplia 4x sem suavizar.
import math, os, sys
from PIL import Image
RAIZ = sys.argv[1] if len(sys.argv) > 1 else "."
PASTA = os.path.join(RAIZ, "Assets", "UI", "Pixel"); os.makedirs(PASTA, exist_ok=True)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
def nova(w, h): return Image.new("RGBA", (w, h), (0, 0, 0, 0))
def px(im, x, y, c, a=255):
    if 0 <= x < im.width and 0 <= y < im.height: im.putpixel((x, y), tuple(c[:3]) + (a,))
def ret(im, x0, y0, x1, y1, c, a=255):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1): px(im, x, y, c, a)
def salvar(n, im): im.save(os.path.join(PASTA, n + ".png"))

CIANO, VERMELHO, AMBAR = (82, 212, 232), (232, 70, 96), (255, 190, 90)
FUNDO, FUNDO2, CONTORNO = (14, 18, 34), (22, 30, 52), (6, 8, 16)

# Painel (9-slice, borda de 5 px): fundo azul-noite, borda ciano com cantos chanfrados e sombra.
def painel(nome, borda, fundo, luz):
    im = nova(16, 16)
    ret(im, 1, 0, 14, 15, CONTORNO); ret(im, 0, 1, 15, 14, CONTORNO)
    ret(im, 2, 1, 13, 14, borda); ret(im, 1, 2, 14, 13, borda)
    ret(im, 2, 2, 13, 13, fundo)
    ret(im, 3, 3, 12, 3, luz); ret(im, 3, 3, 3, 12, luz)
    ret(im, 3, 12, 12, 12, CONTORNO, 120); ret(im, 12, 3, 12, 12, CONTORNO, 120)
    salvar(nome, im)
painel("ui_painel", CIANO, FUNDO, (30, 44, 72))
painel("ui_painel_vermelho", VERMELHO, (30, 14, 26), (60, 26, 44))
painel("ui_botao", (54, 112, 146), FUNDO2, (40, 62, 92))
painel("ui_botao_hover", CIANO, (30, 52, 80), (60, 96, 130))
painel("ui_etiqueta", VERMELHO, VERMELHO, (255, 130, 150))

# Chave liga/desliga do firewall
off = nova(22, 11); ret(off, 1, 0, 20, 10, CONTORNO); ret(off, 0, 1, 21, 9, CONTORNO); ret(off, 2, 2, 19, 8, (40, 44, 62))
ret(off, 3, 3, 9, 7, (150, 156, 172)); ret(off, 3, 3, 9, 3, (200, 204, 216)); ret(off, 13, 5, 17, 5, (90, 96, 118)); salvar("ui_chave_off", off)
on = nova(22, 11); ret(on, 1, 0, 20, 10, CONTORNO); ret(on, 0, 1, 21, 9, CONTORNO); ret(on, 2, 2, 19, 8, (36, 128, 116))
ret(on, 12, 3, 18, 7, (220, 255, 246)); ret(on, 12, 3, 18, 3, (255, 255, 255)); ret(on, 4, 5, 8, 5, (120, 220, 200)); salvar("ui_chave_on", on)

# Post-it (branco, a cor vem do Image) com dobra no canto
pi = nova(20, 18); ret(pi, 0, 0, 19, 17, (255, 255, 255)); ret(pi, 0, 0, 19, 1, (232, 232, 232))
for i in range(4): ret(pi, 19 - i, 17 - (3 - i), 19, 17 - (3 - i), (0, 0, 0), 0)
for i in range(4): px(pi, 16 + i, 17 - i, (200, 200, 200))
ret(pi, 9, 0, 10, 1, VERMELHO); salvar("ui_postit", pi)

# Cursor ">" piscante, neurônio pixelado e scanline
cur = nova(5, 7)
for (x, y) in [(0, 0), (1, 1), (2, 2), (3, 3), (2, 4), (1, 5), (0, 6), (0, 1), (1, 2), (2, 3), (1, 4), (0, 5)]: px(cur, x, y, (255, 255, 255))
salvar("ui_cursor", cur)
n = nova(9, 9)
for y in range(9):
    for x in range(9):
        d = math.hypot(x - 4, y - 4) / 4.5
        if d < .45: px(n, x, y, (255, 255, 255))
        elif d < 1 and (1 - d) * 16 > BAYER[y % 4][x % 4]: px(n, x, y, (255, 255, 255), 150)
salvar("ui_neuronio", n)
s = nova(1, 4); s.putpixel((0, 3), (0, 0, 0, 255)); s.putpixel((0, 2), (0, 0, 0, 90)); salvar("ui_scanline", s)
# Vinheta de CRT pixelada (64x36, ampliada para a tela inteira)
v = nova(64, 36)
for y in range(36):
    for x in range(64):
        d = math.hypot((x - 31.5) / 32, (y - 17.5) / 18)
        t = max(0, (d - .6) / .5)
        if t > 0 and t * 16 > BAYER[y % 4][x % 4]: px(v, x, y, (0, 0, 0), int(min(1, t) * 200))
salvar("ui_vinheta", v)
# Moldura de "check" e "x" para feedback
ok = nova(7, 6)
for (x, y) in [(0, 3), (1, 4), (2, 5), (3, 4), (4, 3), (5, 2), (6, 1), (5, 1), (6, 0)]: px(ok, x, y, (120, 230, 150))
salvar("ui_ok", ok)
# Papel (post-it/cartão) em 9-slice, com sombra embaixo; a cor vem do Image.
pa = nova(16, 16); ret(pa, 0, 0, 15, 13, (255, 255, 255)); ret(pa, 0, 0, 15, 0, (236, 236, 236))
ret(pa, 1, 14, 15, 15, (0, 0, 0), 70); ret(pa, 15, 1, 15, 13, (210, 210, 210)); ret(pa, 0, 13, 15, 13, (205, 205, 205))
salvar("ui_papel", pa)
# Alfinete
al = nova(5, 5); ret(al, 1, 0, 3, 3, (220, 50, 70)); ret(al, 0, 1, 4, 2, (220, 50, 70)); px(al, 1, 1, (255, 150, 160)); px(al, 2, 4, (60, 60, 70)); salvar("ui_alfinete", al)
# Caixa de código (terminal)
cc = nova(16, 16); ret(cc, 0, 0, 15, 15, CONTORNO); ret(cc, 1, 1, 14, 14, (40, 90, 80)); ret(cc, 2, 2, 13, 13, (8, 14, 16)); salvar("ui_codigo", cc)
print("ok")
