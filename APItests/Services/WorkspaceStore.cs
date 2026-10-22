using APItests.Models;

namespace APItests.Services;

// Shared demonstration workspace. Restarting the server resets the data.
public sealed class WorkspaceStore
{
    private readonly object _gate = new();
    private readonly List<PersonnelTest> _tests = [];
    private readonly List<Employee> _employees = [];
    private readonly List<Assignment> _assignments = [];
    public static readonly string[] Departments = ["Продажи", "Разработка", "Маркетинг", "HR", "Все отделы"];
    public PersonnelTest[] Tests { get { lock (_gate) return _tests.ToArray(); } }
    public Employee[] Employees { get { lock (_gate) return _employees.ToArray(); } }
    public Assignment[] Assignments { get { lock (_gate) return _assignments.ToArray(); } }
    public PersonnelTest? FindTest(Guid id) => Tests.FirstOrDefault(t => t.Id == id);
    public Employee? FindEmployee(Guid id) => Employees.FirstOrDefault(e => e.Id == id);
    public static string Status(Assignment a) => a.CompletedAt != null ? "Завершён" : a.Deadline.Date < DateTime.Today ? "Просрочен" : "Назначен";

    public WorkspaceStore(DemoQuestionGenerator generator)
    {
        (string Name, string Role, string Dept, string Email, string Color)[] people = [
            ("Анна Смирнова", "Менеджер по продажам", "Продажи", "a.smirnova@forma.team", "peach"),
            ("Михаил Волков", "Frontend-разработчик", "Разработка", "m.volkov@forma.team", "violet"),
            ("Екатерина Орлова", "Маркетолог", "Маркетинг", "e.orlova@forma.team", "green"),
            ("Дмитрий Соколов", "Менеджер по продажам", "Продажи", "d.sokolov@forma.team", "blue"),
            ("Мария Козлова", "HR-менеджер", "HR", "m.kozlova@forma.team", "pink"),
            ("Александр Морозов", "Backend-разработчик", "Разработка", "a.morozov@forma.team", "green"),
            ("Полина Лебедева", "Контент-маркетолог", "Маркетинг", "p.lebedeva@forma.team", "violet"),
            ("Иван Петров", "Руководитель отдела", "Продажи", "i.petrov@forma.team", "peach")
        ];
        _employees.AddRange(people.Select(e => new Employee(Guid.NewGuid(), e.Name, e.Role, e.Dept, e.Email, e.Color)));
        (string Title, string Description, string Dept, string Level, int Minutes, bool Ai)[] definitions = [
            ("Искусство работы с клиентом", "Выявление потребностей, работа с возражениями и забота о клиенте.", "Продажи", "Средний", 20, true),
            ("Информационная безопасность", "Защита данных и безопасная работа с корпоративными сервисами.", "Все отделы", "Базовый", 15, true),
            ("Добро пожаловать в команду", "Всё, что важно знать новому сотруднику в первые недели.", "HR", "Базовый", 15, false),
            ("Обратная связь и лидерство", "Коммуникация, управление задачами и развитие команды.", "Все отделы", "Продвинутый", 25, true),
            ("Клиентский сервис: практика", "Разбираем сложные ситуации и находим решения для клиента.", "Продажи", "Средний", 20, false),
            ("Эффективная работа в команде", "Общие цели, передача задач и ответственность за результат.", "Разработка", "Средний", 15, true)
        ];
        var day = DateTime.Today;
        _tests.AddRange(definitions.Select((t, i) => new PersonnelTest(Guid.NewGuid(), t.Title, t.Description, t.Dept, t.Level,
            t.Minutes, 70, day.AddDays(-i * 3 - 1), generator.Generate(t.Title, t.Dept, t.Level, 8, ""), t.Ai)));
        for (var i = 0; i < 24; i++)
        {
            var test = _tests[i % _tests.Count];
            var employee = _employees[i % _employees.Count];
            var completed = i < 16;
            var score = new[] { 88, 100, 75, 63, 88, 100, 50, 88 }[i % 8];
            _assignments.Add(new Assignment(Guid.NewGuid(), test.Id, employee.Id, day.AddDays(-12 + i / 3),
                day.AddDays(i == 23 ? -1 : 5 + i % 4), completed ? day.AddDays(-6 + i / 3).AddHours(10 + i % 5) : null,
                completed ? score : null));
        }
    }

    public PersonnelTest AddTest(string title, string description, string department, string level, int duration, int passScore, Question[] questions)
    {
        if (string.IsNullOrWhiteSpace(title) || questions.Length == 0) throw new ArgumentException("Добавьте название и вопросы теста.");
        if (questions.Any(q => string.IsNullOrWhiteSpace(q.Text) || q.Options.Length != 4 || q.Options.Any(string.IsNullOrWhiteSpace) || q.CorrectIndex is < 0 or > 3))
            throw new ArgumentException("Заполните вопросы, четыре варианта ответа и выберите правильный ответ.");
        var test = new PersonnelTest(Guid.NewGuid(), title.Trim(), description.Trim(), department, level, Math.Clamp(duration, 5, 120), Math.Clamp(passScore, 1, 100), DateTime.Now, questions, true);
        lock (_gate) _tests.Insert(0, test);
        return test;
    }

    public Employee AddEmployee(string name, string role, string department, string email)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(role) || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email.Trim())
            throw new ArgumentException("Заполните имя, должность и корректный рабочий email.");
        lock (_gate)
        {
            if (_employees.Any(e => e.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase))) throw new ArgumentException("Сотрудник с таким email уже добавлен.");
            var person = new Employee(Guid.NewGuid(), name.Trim(), role.Trim(), department, email.Trim(), new[] { "violet", "peach", "green", "blue" }[_employees.Count % 4]);
            _employees.Add(person);
            return person;
        }
    }

    public int Assign(Guid testId, IEnumerable<Guid> employees, DateTime deadline)
    {
        if (deadline.Date < DateTime.Today) throw new ArgumentException("Срок прохождения не может быть в прошлом.");
        lock (_gate)
        {
            if (!_tests.Any(t => t.Id == testId)) throw new ArgumentException("Тест не найден.");
            var count = 0;
            foreach (var id in employees.Distinct())
            {
                if (!_employees.Any(e => e.Id == id) || _assignments.Any(a => a.TestId == testId && a.EmployeeId == id && a.CompletedAt == null && a.Deadline.Date >= DateTime.Today)) continue;
                _assignments.Insert(0, new Assignment(Guid.NewGuid(), testId, id, DateTime.Now, deadline.Date));
                count++;
            }
            return count;
        }
    }

    public Assignment Complete(Guid assignmentId, int[] answers)
    {
        lock (_gate)
        {
            var index = _assignments.FindIndex(a => a.Id == assignmentId);
            if (index < 0) throw new ArgumentException("Назначение не найдено.");
            var assignment = _assignments[index];
            if (assignment.CompletedAt != null) throw new ArgumentException("Этот тест уже пройден.");
            if (assignment.Deadline.Date < DateTime.Today) throw new ArgumentException("Срок прохождения истёк. Создайте новое назначение.");
            var test = _tests.Single(t => t.Id == assignment.TestId);
            if (answers.Length != test.Questions.Length || answers.Any(a => a is < 0 or > 3)) throw new ArgumentException("Ответьте на все вопросы.");
            var score = (int)Math.Round(100.0 * test.Questions.Where((q, i) => q.CorrectIndex == answers[i]).Count() / test.Questions.Length);
            return _assignments[index] = assignment with { Score = score, CompletedAt = DateTime.Now, Answers = answers.ToArray() };
        }
    }
}
