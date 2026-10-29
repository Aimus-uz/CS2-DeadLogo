# CS2-DeadLogo

Логотип сервера в центре экрана для мёртвых игроков и наблюдателей.
При возрождении логотип пропадает.

## Требования
- CounterStrikeSharp

## Установка
1. Скачайте `CS2-DeadLogo-<версия>.zip` из [Releases](../../releases).
2. Распакуйте в `game/csgo/`.
3. Перезапустите сервер. Конфиг создастся в:
   `addons/counterstrikesharp/configs/plugins/DeadLogo/DeadLogo.json`
4. Укажите `LogoUrl` (прямая https-ссылка на PNG) и выполните:
   `css_plugins reload DeadLogo`

## Команды
| Команда | Кто | Описание |
|---|---|---|
| `!logo` / `css_logo` | игрок | Включить/выключить логотип для себя |
| `css_deadlogo_pause <slot\|all> <сек>` | сервер / @css/root | Поставить логотип на паузу |

## Совместимость с HUD/меню
Центр экрана (center HTML) в CS2 один на всех: если два плагина пишут туда одновременно, они перебивают друг друга.
- `PauseOnCommands`: при вводе этих команд (`!sprays`, `!ws`...) логотип у игрока скрывается на `PauseSeconds`.
- `DelayAfterDeath` / `MaxShowTime`: можно разделить время с другим HUD.
- `ClearOnSpawn`: стирает только логотип, показанный этим плагином.
- Другие плагины могут вызвать `css_deadlogo_pause <slot> <сек>`.

## Откат версии
Все старые версии лежат в Releases. Скачайте нужный ZIP и замените файлы.
