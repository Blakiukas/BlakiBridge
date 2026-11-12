namespace BlakiBridge;

internal static class UiText
{
    internal static string Language { get; set; } = "EN";
    internal static string Pick(string en, string lt, string ru) => Language switch { "LT" => lt, "RU" => ru, _ => en };
    private static readonly Dictionary<string, (string Lt, string Ru)> Texts = new()
    {
        ["Enabled"] = ("Įjungta", "Включено"),
        ["Adjust"] = ("Reguliuoti", "Настроить"),
        ["Optimize mouse speed"] = ("Vienodinti pelės greitį", "Выравнивать скорость мыши"),
        ["Use physical gap"] = ("Įskaityti fizinį tarpą", "Учитывать зазор"),
        ["Start with Windows"] = ("Paleisti su Windows", "Запуск с Windows"),
        ["How to use"] = ("Instrukcija", "Инструкция"),
        ["Back to screens"] = ("Grįžti prie ekranų", "Назад к экранам"),
        ["Save"] = ("Išsaugoti", "Сохранить"), ["Saved"] = ("Išsaugota", "Сохранено"),
        ["Cancel"] = ("Atšaukti", "Отмена"), ["Next"] = ("Toliau", "Далее"), ["Done"] = ("Baigti", "Готово"),
        ["Display"] = ("Ekranas", "Экран"), ["Physical measurements"] = ("Fiziniai matmenys", "Физические размеры"),
        ["Physical gap"] = ("Fizinis tarpas", "Физический зазор"),
        ["Distance between screens"] = ("Atstumas tarp ekranų", "Расстояние между экранами"),
        ["Measure from one lit image edge to the other. Include both bezels and the space between the cases."] = ("Matuokite nuo vieno šviečiančio vaizdo krašto iki kito. Įtraukite abu rėmelius ir tarpą tarp korpusų.", "Измеряйте между краями видимого изображения. Включите обе рамки и промежуток между корпусами."),
        ["Screen-to-screen distance:"] = ("Atstumas tarp vaizdų:", "Между краями изображения:"),
        ["Diagram and bezels are schematic. Accurate screen sizes improve travel precision."] = ("Vaizdas ir rėmeliai scheminiai. Tikslūs ekranų matmenys pagerina judėjimo tikslumą.", "Изображение и рамки показаны схематично. Точные размеры экранов повышают точность движения."),
        ["Active image width"] = ("Vaizdo plotis", "Ширина изображения"),
        ["Active image height"] = ("Vaizdo aukštis", "Высота изображения"),
        ["Left bezel"] = ("Kairysis rėmelis", "Левая рамка"), ["Right bezel"] = ("Dešinysis rėmelis", "Правая рамка"),
        ["Top bezel"] = ("Viršutinis rėmelis", "Верхняя рамка"), ["Bottom bezel"] = ("Apatinis rėmelis", "Нижняя рамка"),
        ["Detected details"] = ("Aptikti duomenys", "Данные монитора"),
        ["Optional precision · all values in mm"] = ("Papildomas tikslumas · visi dydžiai mm", "Уточнение размеров · все значения в мм"),
        ["Hover over a parameter to locate it on the preview. Save applies these dimensions to the diagram and speed."] = ("Užveskite pelę ant parametro, kad matytumėte jo vietą. Išsaugoti matmenys taikomi schemai ir greičiui.", "Наведите на параметр, чтобы увидеть его на схеме. Сохранённые размеры применяются к схеме и скорости."),
        ["Your saved physical measurements are shown below."] = ("Žemiau rodomi jūsų išsaugoti matmenys.", "Ниже показаны ваши сохранённые размеры."),
        ["Starting values are estimates. Exact measurements are optional."] = ("Pradinės reikšmės yra apytikslės. Tikslūs matmenys neprivalomi.", "Начальные значения приблизительны. Точные измерения необязательны."),
        ["Open BlakiBridge"] = ("Atidaryti BlakiBridge", "Открыть BlakiBridge"),
        ["Adjust screens"] = ("Reguliuoti ekranus", "Настроить экраны"),
        ["Disable mouse control"] = ("Išjungti pelės valdymą", "Отключить управление мышью"),
        ["Exit"] = ("Išeiti", "Выход"),
        ["Exit Little Big Mouse, then turn Enabled on."] = ("Uždarykite Little Big Mouse, tada pažymėkite „Įjungta“.", "Закройте Little Big Mouse, затем отметьте «Включено»."),
        ["Place screens beside each other to create a connection."] = ("Padėkite ekranus greta, kad sukurtumėte perėjimą.", "Расположите экраны рядом, чтобы создать переход."),
        ["Ctrl + Alt + F12 is unavailable."] = ("Ctrl + Alt + F12 nepasiekiamas.", "Ctrl + Alt + F12 недоступно."),
        ["Display setup changed. Align your screens again."] = ("Ekranų išdėstymas pasikeitė. Suderinkite ekranus iš naujo.", "Конфигурация экранов изменилась. Снова совместите экраны."),
        ["Cursor recovery protection could not start."] = ("Nepavyko paleisti žymeklio atkūrimo apsaugos.", "Не удалось запустить защиту восстановления курсора."),
        ["Background CPU"] = ("CPU fone", "CPU в фоне"),
        ["Resource readings unavailable"] = ("Išteklių rodmenys nepasiekiami", "Данные о ресурсах недоступны"),
        ["Drag screens and their corners to match your desk."] = ("Tempkite ekranus ir jų kampus pagal savo stalą.", "Расположите экраны и измените их размеры за углы."),
        ["Arrange the screens visually to match your real monitor setup."] = ("Išdėstykite ekranus schemoje taip, kaip jūsų monitoriai stovi iš tikrųjų.", "Расположите экраны на схеме так, как ваши мониторы расположены в реальности."),
        ["Speed: based on screen proportions."] = ("Greitis pagal ekranų proporcijas.", "Скорость по пропорциям экранов."),
        ["Speed: Windows default."] = ("Greitis: Windows numatytasis.", "Скорость: стандартная Windows."),
        ["Display setup changed. Restart BlakiBridge to refresh connections."] = ("Ekranų išdėstymas pasikeitė. Paleiskite BlakiBridge iš naujo.", "Конфигурация экранов изменилась. Перезапустите BlakiBridge."),
        ["Esc is in use by another app. Turn off Adjust here to hide the lines."] = ("Esc naudoja kita programa. Paspauskite „Baigti reguliavimą“, kad paslėptumėte žymas.", "Esc используется другой программой. Снимите «Настроить», чтобы скрыть линии."),
        ["BlakiBridge is still running"] = ("BlakiBridge tebeveikia", "BlakiBridge продолжает работать"),
        ["Open it from the system tray. Use Exit to stop it completely."] = ("Atidarykite iš sistemos dėklo. Norėdami išjungti, pasirinkite „Išeiti“.", "Откройте из области уведомлений. Для полного закрытия выберите «Выход»."),
    };
    internal static string T(string text) => Texts.TryGetValue(text, out var t) ? Pick(text, t.Lt, t.Ru) : text;
    internal static void Apply(Control root)
    {
        // Dialogs are translated once, before showing; numeric/model data stay intact.
        if (root is Label or Button or CheckBox or Form) root.Text = string.Join("\n", root.Text.Split('\n').Select(T));
        foreach (Control child in root.Controls) Apply(child);
    }
}
