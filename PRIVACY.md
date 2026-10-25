# BlakiBridge privacy information

Updated: 25 October 2026 · Applies to BlakiBridge 1.0.3

## English

BlakiBridge processes monitor configuration and mouse movement locally to calibrate transitions between screens. No account is required. The application code does not include analytics, advertising, an automatic updater or automatic uploading of settings or diagnostic logs to the author.

Settings include language, monitor identifiers and layout, physical measurements, calibration and mouse-speed preferences. Local diagnostic logs can include timestamps, process IDs, the foreground application's process name, display information, cursor coordinates, transition events and error details. Errors may contain local file paths. Normal diagnostic logging rotates an approximately 1 MiB events file and retains one previous file. The program does not record typed text or the contents of other applications' windows.

- **Setup:** settings are normally in `%LOCALAPPDATA%\BlakiBridge`; logs are in its `diagnostics` subfolder.
- **Portable:** settings and diagnostics are in `Data` next to the executable.
- **Microsoft Store:** Windows package storage redirects the local data location, normally under `%LOCALAPPDATA%\Packages\Blakiukas.BlakiBridge_sdefccrkgb0gm\LocalCache\Local\BlakiBridge`.

Close the program before deleting local data. Deleting `config.json` and its `.bak` copy resets settings; deleting `diagnostics` removes local logs. Setup uninstallation can remove settings when selected, but retains diagnostics. Removing the extracted portable folder removes its local data too. Preserve any settings you want to keep before removal.

The contact and donation controls open your chosen mail application or browser only when clicked. Email providers, donation services, Microsoft Store, GitHub and other distribution platforms process data under their own policies. This notice describes BlakiBridge, not Windows or those services.

If you email `blaki.support@gmail.com`, the author receives your address, message and any files you choose to send, for handling your support request. Diagnostic files are not sent automatically: review them before sharing. Support correspondence is retained only as needed for the request and any applicable legal obligations; contact the same address to ask about access or deletion. Do not send passwords or unrelated personal information.

## Lietuvių

„BlakiBridge“ monitorių duomenis ir pelės judėjimą apdoroja kompiuteryje. Paskyros nereikia. Programos kode nėra reklamos, analitikos, automatinio atnaujinimo ar automatinio nustatymų bei diagnostikos siuntimo autoriui.

Nustatymuose saugomi kalba, monitorių identifikatoriai, išdėstymas, matmenys, kalibravimas ir greitis. Vietinėje diagnostikoje gali būti laikas, procesų numeriai, aktyvios programos proceso pavadinimas, ekranų duomenys, kursoriaus koordinatės ir klaidos su vietiniais keliais. Įprastas maždaug 1 MiB žurnalas keičiamas nauju, paliekant vieną ankstesnį. Įvedamas tekstas ir kitų programų langų turinys neįrašomi.

Setup duomenys įprastai saugomi `%LOCALAPPDATA%\BlakiBridge`, portable — `Data` šalia programos; Store vieta nurodyta aukščiau. Prieš trindami duomenis uždarykite programą. `config.json` ir `.bak` pašalinimas atkuria pradinius nustatymus; `diagnostics` pašalinimas ištrina žurnalus. Setup pašalinimas gali ištrinti pasirinktus nustatymus, bet diagnostiką palieka.

Kontaktų ir paramos mygtukai atveria pašto programą arba naršyklę tik paspaudus. Išorinėms paslaugoms taikomos jų privatumo taisyklės. Parašius `blaki.support@gmail.com`, autorius gauna jūsų adresą, laišką ir pasirinktus priedus pagalbos užklausai nagrinėti. Diagnostika automatiškai nesiunčiama — prieš dalindamiesi ją peržiūrėkite. Laiškai saugomi tiek, kiek reikia užklausai ir teisės reikalavimams; tuo pačiu adresu galima kreiptis dėl duomenų ar jų ištrynimo.

## Русский

BlakiBridge обрабатывает данные мониторов и движение мыши локально. Учётная запись не нужна. Код приложения не содержит рекламы, аналитики, автоматического обновления или автоматической отправки настроек и журналов автору.

Настройки содержат язык, идентификаторы мониторов, расположение, размеры, калибровку и скорость. Локальные журналы могут содержать время, идентификаторы процессов, имя процесса активного приложения, сведения об экранах, координаты курсора и ошибки с локальными путями. Обычный журнал размером около 1 МиБ заменяется новым с сохранением одной предыдущей копии. Вводимый текст и содержимое окон других приложений не записываются.

Setup обычно хранит данные в `%LOCALAPPDATA%\BlakiBridge`, portable — в соседней папке `Data`; путь Store указан выше. Перед удалением данных закройте программу. Удаление `config.json` и `.bak` сбрасывает настройки; удаление `diagnostics` удаляет журналы. Деинсталлятор Setup может удалить настройки по выбору пользователя, но сохраняет диагностику.

Кнопки связи и поддержки открывают почту или браузер только после нажатия. Внешние сервисы имеют собственные правила конфиденциальности. При обращении на `blaki.support@gmail.com` автор получает ваш адрес, письмо и выбранные вложения для помощи. Диагностика не отправляется автоматически; проверьте её перед отправкой. Переписка хранится по необходимости для запроса и требований закона. По тому же адресу можно запросить доступ к данным или их удаление.
