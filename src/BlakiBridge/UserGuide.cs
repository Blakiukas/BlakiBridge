namespace BlakiBridge;

public sealed class UserGuide : UserControl
{
    internal readonly BackButton Back = new() { ForeColor = Color.FromArgb(255, 200, 70), Emphasized = true };
    private readonly TableLayoutPanel content = new() { Dock = DockStyle.Fill, ColumnCount=2,RowCount=2,Padding=new Padding(0,32,0,0) };
    private readonly FlowLayoutPanel steps=new(){Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0,0,20,0)};
    private readonly FlowLayoutPanel optional=new(){Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=Padding.Empty};
    private readonly SmoothLabel adminNote = new() { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0) };
    private int itemCount;
    private readonly ResourceExplanationLabel title=new(){Dock=DockStyle.Fill,Padding=new Padding(16,0,0,0),TextAlign=ContentAlignment.MiddleLeft,Font=new Font("Segoe UI",11,FontStyle.Bold)};
    public UserGuide()
    {
        AutoScroll = false; Padding = new Padding(22, 12, 22, 12);
        BackColor = AppTheme.Background; ForeColor = Color.White;
        var navigation = new Panel { Dock = DockStyle.Top, Height = 44 };
        Back.Dock=DockStyle.Left;var titleHost=new Panel{Dock=DockStyle.Fill,Padding=new Padding(8,0,0,0)};titleHost.Controls.Add(title);navigation.Controls.Add(titleHost);navigation.Controls.Add(Back);
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,60));content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,40));content.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        content.Controls.Add(steps,0,0);content.Controls.Add(optional,1,0);
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.Controls.Add(adminNote,0,1);content.SetColumnSpan(adminNote,2);
        Controls.AddRange([content, navigation]);
        Resize += (_, _) => FitText();
        RefreshLanguage();
    }
    internal void RefreshLanguage()
    {
        content.SuspendLayout();
        foreach(var group in new[]{steps,optional}){foreach(Control child in group.Controls.Cast<Control>().ToArray())child.Dispose();group.Controls.Clear();}itemCount=0;
        title.Text=UiText.Pick("How to use BlakiBridge", "Kaip naudoti BlakiBridge", "Как пользоваться BlakiBridge");itemCount=1;
        Add(UiText.Pick("1. Enable the program.", "1. Įjunkite programą.", "1. Включите программу."));
        Add(UiText.Pick("2. Click ← to return to the screen layout. Drag the monitor shapes to match the arrangement on your desk. Resize a shape by dragging its bottom-right corner.", "2. Rodykle ← grįžkite į ekranų schemą. Nutempkite monitorių atvaizdus taip, kaip monitoriai išdėstyti ant jūsų stalo. Atvaizdo dydį keiskite tempdami jo apatinį dešinį kampą.", "2. Нажмите ←, чтобы вернуться к схеме экранов. Расположите изображения мониторов так, как они стоят на вашем столе. Чтобы изменить размер изображения, потяните за его правый нижний угол."));
        Add(UiText.Pick("3. Turn on Adjust.", "3. Įjunkite „Reguliuoti“.", "3. Включите «Настроить»."));
        Add(UiText.Pick("4. Align the coloured line tips at the screen edges. Each monitor pair has its own colour.", "4. Sulygiuokite spalvotų linijų smaigalius ties ekranų kraštais. Kiekviena monitorių pora pažymėta atskira spalva.", "4. Совместите цветные метки на экранах. Каждая пара мониторов отмечена своим цветом."));
        Add(UiText.Pick("5. Click Finish adjusting. Settings save automatically. Close the window to keep the program running in the system tray.", "5. Paspauskite „Baigti reguliavimą“. Nustatymai išsaugomi automatiškai. Uždarius langą programa toliau veikia sistemos dėkle.", "5. Нажмите «Завершить настройку». Настройки сохраняются автоматически. После закрытия окна программа продолжает работать в системном трее."));
        Add(UiText.Pick("Optional", "Papildomai", "Дополнительно"), true);
        Add(UiText.Pick("• Start with Windows: enable automatic startup.\n• Use physical gap: enter distances between screens.\n• Select a screen, then open Dimensions, Mouse speed or Physical gap.\n• Optimize mouse speed: reduce speed differences between screens.", "• „Paleisti su Windows“ — automatinis paleidimas.\n• „Įskaityti fizinį tarpą“ — atstumai tarp ekranų.\n• Pažymėkite monitorių ir atverkite „Matmenys“, „Pelės greitis“ arba „Fizinis tarpas“.\n• „Vienodinti pelės greitį“ — mažesnis greičio skirtumas ekranuose.", "• «Запуск с Windows» — автоматический запуск.\n• «Учитывать зазор» — расстояния между экранами.\n• Выберите монитор, затем откройте «Размеры», «Скорость мыши» или «Физический зазор».\n• «Выравнивать скорость мыши» — уменьшение разницы скорости."));
        Add(UiText.Pick("If the pointer moves incorrectly: Ctrl + Alt + F12.", "Jei pelė juda netinkamai: Ctrl + Alt + F12.", "Если указатель движется неправильно: Ctrl + Alt + F12."), true);
        Add(UiText.Pick("Use only one mouse correction program at a time.","Vienu metu naudokite tik vieną pelės judėjimą koreguojančią programą.","Используйте только одну программу коррекции движения мыши одновременно."));
        adminNote.Text = UiText.Pick(
            "If an app running as administrator opens on a secondary monitor when Windows starts and remains active, automatic mouse correction may not work until you click an empty area of the main monitor’s desktop. This is a Windows security restriction.",
            "Jei paleidus „Windows“ papildomame monitoriuje atsidaro ir lieka aktyvi programa, veikianti administratoriaus teisėmis, automatinė pelės korekcija gali neveikti, kol nespustelėsite tuščios pagrindinio monitoriaus darbalaukio vietos. Tai „Windows“ saugos apribojimas.",
            "Если при запуске Windows на дополнительном мониторе открывается и остаётся активной программа с правами администратора, автоматическая коррекция мыши может не работать, пока вы не щёлкнете по пустому месту рабочего стола основного монитора. Это ограничение безопасности Windows.");
        FitText();
        content.ResumeLayout(true);
    }
    private void Add(string text, bool emphasis = false)
    {
        var label = new SmoothLabel { Text = text, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
        if (emphasis) label.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        (itemCount++<6?steps:optional).Controls.Add(label);
    }
    private void FitText()
    {
        title.Padding=new Padding((int)(16*DeviceDpi/96f)+AppText.MeasureText("MMMM",title.Font).Width,0,0,0);
        int width = Math.Max(80, ClientSize.Width - Padding.Horizontal);
        adminNote.MaximumSize = new Size(width, 0);
        foreach(var group in new[]{steps,optional})foreach(Label label in group.Controls)label.MaximumSize=new Size(Math.Max(80,(int)(width*(group==steps?.6:.4))-group.Margin.Horizontal-6),0);
    }
}
