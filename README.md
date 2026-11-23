# Bug In The Neural Network

*Um escape room sobre saúde mental e computação.*

Jogo 3D feito em Unity para a disciplina de Computação Gráfica e Realidade Virtual (CGRV) da Universidade de Fortaleza (Unifor), 2026.

Cris estuda Ciência da Computação e tem uma apresentação importante amanhã às 9h. A tarde está passando e nada sai do lugar. O jogador explora o quarto de Cris e resolve três puzzles, cada um baseado em um conceito de Computação:

| Puzzle | Objeto | Conceito |
|---|---|---|
| 1 | Escrivaninha com post-its | Fila de prioridade |
| 2 | Mural de cortiça | Laço infinito e condição de parada |
| 3 | Celular na cama | Firewall e regras de filtragem |
| Final | Cortina | Abrir a cortina e pedir ajuda |

A cada puzzle resolvido, uma parte do quarto se arruma sozinha. Se o jogador ficar parado muito tempo, ouve um coração que acelera aos poucos. O jogo termina indicando o CVV (188, cvv.org.br).

## Equipe

- **Paulo Alencar:** programação e arquitetura
- **Maria Clara Andrade:** narrativa, textos e GDD
- **Enzo Lima:** arte e ambientação 3D
- **Natan Shinmon:** personagem, interface e testes

## Como abrir

1. Instale o **Unity 6000.6.2f1** (Unity 6), com o Universal Render Pipeline.
2. No Unity Hub, clique em **Add project from disk** e escolha esta pasta.
3. Abra a cena `Assets/Scenes/SampleScene.unity` e aperte Play.

**Controles:** WASD ou setas para andar, **E** para interagir, mouse nos puzzles e **Esc** para fechar um painel.

## Ferramentas de editor

A cena é montada e ajustada pelos menus em **Bug In The Neural Network** (scripts em `Assets/Editor`). O menu **Versão Cris → Aplicar Tudo** aplica a versão atual: personagem, fim de tarde, textos, arrumação do quarto, batimentos e UI pixelada.

Os sprites da interface pixel são gerados por `Tools/gerar_ui_pixel.py`, que precisa do Pillow.

## Créditos de terceiros

- Móveis: [Furniture Kit da Kenney](https://kenney.nl) (CC0).
- Fontes: VT323 e Press Start 2P, ambas sob a SIL Open Font License (`Assets/UI/Pixel/Fontes/OFL.txt`).
