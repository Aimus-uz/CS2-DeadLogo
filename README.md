# GamePointLogo

Баннер GAMEPOINT в центре экрана для мёртвых игроков и наблюдателей.
При возрождении пропадает. KZ и Bhop: не ставится (в центре таймер).

Требования: CounterStrikeSharp 1.0.376+, .NET 10.

## Сборка
Actions → последний зелёный запуск → Artifacts → `GamePointLogo-1.0.X`

## Установка
1. Распаковать архив в `game/csgo/`.
2. Из `configs/plugins/GamePointLogo/templates/` скопировать нужный файл
   на уровень выше как `GamePointLogo.json`.
3. Указать `LogoUrl` (публичная ссылка на PNG).
4. `css_plugins reload GamePointLogo`

| Шаблон | Сервер |
|---|---|
| mix.json | 5x5 MatchZy |
| retake.json | Retake |
| mini.json | Мини-игры |
| awp.json | AWP Lego |
| aim.json | Aim USP, Aim Redline |
| surf.json | Surf (по желанию) |

## Команды
| Команда | Кто | Что делает |
|---|---|---|
| `!logo` | игрок | вкл/выкл баннер для себя |
| `css_gplogo_pause <slot\|all> <сек>` | сервер / @css/root | пауза |
| `css_gplogo_resume <slot\|all>` | сервер / @css/root | снять паузу |
