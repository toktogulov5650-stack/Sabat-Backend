using Microsoft.EntityFrameworkCore;
using Sabat.Domain;

namespace Sabat.Infrastructure.Persistence;

public sealed class InitialDataSeeder(AppDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingSlugs = await dbContext.NewsArticles
            .Select(article => article.Slug)
            .ToHashSetAsync(cancellationToken);
        var articles = CreateArticles()
            .Where(article => !existingSlugs.Contains(article.Slug))
            .ToArray();

        if (articles.Length == 0)
        {
            return;
        }

        dbContext.NewsArticles.AddRange(articles);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<NewsArticle> CreateArticles() =>
    [
        Article(
            "new-season",
            "initiatives",
            new DateTimeOffset(2026, 6, 18, 9, 0, 0, TimeSpan.Zero),
            Translation("ru", "Инициативы", "Новый сезон общественных проектов Sabat", "Запускаем встречи, открытые площадки и новые форматы совместной работы.",
                "В новом сезоне мы сосредоточимся на небольших локальных инициативах, открытом диалоге и проектах, которые приносят практическую пользу людям.",
                "Программа объединит открытые встречи, командные обсуждения и практическую работу. Участники смогут определить важную для своего сообщества задачу и вместе найти реалистичный способ её решения.",
                "Каждая инициатива пройдёт путь от первой идеи до понятного результата. Для нас важно, чтобы этот опыт помогал молодым людям увереннее действовать, слышать друг друга и брать ответственность за общее дело."),
            Translation("ky", "Демилгелер", "Sabat коомдук долбоорлорунун жаңы сезону", "Жолугушууларды, ачык аянтчаларды жана биргелешкен иштин жаңы форматтарын баштайбыз.",
                "Жаңы сезондо чакан жергиликтүү демилгелерге, ачык диалогго жана адамдарга практикалык пайда алып келген долбоорлорго басым жасайбыз.",
                "Программа ачык жолугушууларды, командалык талкууларды жана практикалык ишти бириктирет. Катышуучулар коомчулук үчүн маанилүү маселени аныктап, аны чечүүнүн реалдуу жолун чогуу табышат.",
                "Ар бир демилге биринчи идеядан түшүнүктүү натыйжага чейинки жолду басып өтөт. Бул тажрыйба жаштарга ишенимдүү аракеттенүүгө, бири-бирин угууга жана жалпы иш үчүн жоопкерчилик алууга жардам берет."),
            Translation("en", "Initiatives", "A new season of Sabat community projects", "New meetings, open platforms and formats for working together are about to begin.",
                "This season, we are focusing on small local initiatives, open dialogue and projects that deliver practical value for people.",
                "The programme will combine open meetings, team discussions and hands-on work. Participants will identify an issue that matters to their community and develop a realistic way to address it together.",
                "Each initiative will move from an initial idea to a clear result. We want this experience to help young people act with confidence, listen to one another and take responsibility for a shared purpose.")),
        Article(
            "community-meeting",
            "events",
            new DateTimeOffset(2026, 6, 4, 9, 0, 0, TimeSpan.Zero),
            Translation("ru", "События", "Открытая встреча сообщества Sabat", "Участники обсудили идеи, которые могут улучшить жизнь вокруг нас.",
                "В центре разговора были взаимная поддержка, доступные общественные пространства и способы превращать хорошие идеи в реальные действия. Особое внимание уделили тому, как сделать участие в общественных проектах понятным и доступным для разных людей.",
                "Участники делились наблюдениями и обсуждали, какие небольшие изменения можно начать уже сейчас. Работа в малых группах помогла превратить общие пожелания в конкретные предложения.",
                "Следующий шаг — собрать команды вокруг выбранных идей и определить, какую поддержку Sabat может предоставить каждой инициативе."),
            Translation("ky", "Окуялар", "Sabat коомчулугунун ачык жолугушуусу", "Катышуучулар айланабыздагы жашоону жакшырта турган идеяларды талкуулашты.",
                "Сүйлөшүүнүн өзөгүндө өз ара колдоо, жеткиликтүү коомдук мейкиндиктер жана жакшы идеяларды реалдуу ишке айлантуу жолдору болду.",
                "Катышуучулар байкоолору менен бөлүшүп, азыртан баштоого мүмкүн болгон чакан өзгөрүүлөрдү талкуулашты.",
                "Кийинки кадам — тандалган идеялардын айланасына командаларды чогултуу жана Sabat кандай колдоо көрсөтө аларын аныктоо."),
            Translation("en", "Events", "An open meeting of the Sabat community", "Participants discussed ideas that can improve everyday life around us.",
                "The conversation focused on mutual support, accessible public spaces and ways to turn good ideas into practical action.",
                "Participants shared observations and discussed small changes that could begin immediately. Working in small groups helped turn broad wishes into concrete proposals.",
                "The next step is to form teams around selected ideas and determine how Sabat can support each initiative.")),
        Article(
            "small-steps",
            "stories",
            new DateTimeOffset(2026, 5, 22, 9, 0, 0, TimeSpan.Zero),
            Translation("ru", "Истории", "Почему большие перемены начинаются с малого", "Пять простых принципов, которые помогают инициативе стать устойчивой.",
                "Начните с понятной задачи, соберите небольшую команду, распределите ответственность, слушайте обратную связь и отмечайте каждый достигнутый результат.",
                "Устойчивая работа редко начинается с большого масштаба. Небольшой, но завершённый шаг помогает проверить идею, увидеть её пользу и понять, что следует улучшить перед продолжением.",
                "Постепенное развитие делает инициативу понятнее для участников, партнёров и тех, ради кого она создаётся."),
            Translation("ky", "Баяндар", "Эмне үчүн чоң өзгөрүүлөр кичине кадамдардан башталат", "Демилгенин туруктуу болушуна жардам берген беш жөнөкөй принцип.",
                "Так милдеттен баштаңыз, чакан команда түзүңүз, жоопкерчиликти бөлүштүрүңүз, пикирлерди угуңуз жана ар бир жетишкен натыйжаны белгилеңиз.",
                "Туруктуу иш сейрек учурда чоң масштабдан башталат. Чакан, бирок аяктаган кадам идеяны текшерүүгө жана анын пайдасын көрүүгө жардам берет.",
                "Акырындык менен өнүгүү демилгени катышуучулар жана өнөктөштөр үчүн түшүнүктүүрөөк кылат."),
            Translation("en", "Stories", "Why meaningful change begins with small steps", "Five simple principles that help an initiative become sustainable.",
                "Start with a clear task, gather a small team, share responsibility, listen to feedback and acknowledge every result along the way.",
                "Sustainable work rarely begins at scale. A small but completed step helps test an idea, demonstrate its value and reveal what should improve.",
                "Gradual development makes an initiative clearer to participants, partners and the people it is designed to serve.")),
        Article(
            "partners",
            "partnerships",
            new DateTimeOffset(2026, 5, 8, 9, 0, 0, TimeSpan.Zero),
            Translation("ru", "Партнёрство", "Sabat открыт к новым партнёрствам", "Приглашаем организации, экспертов и активных людей создавать полезные проекты вместе.",
                "Мы открыты к сотрудничеству со всеми, кому близки идеи взаимного уважения, ответственности и позитивных изменений в обществе.",
                "Партнёры могут участвовать в образовательных программах как эксперты и наставники, поддерживать практические проекты или предлагать новые направления совместной работы.",
                "Нам особенно важны долгосрочные отношения, в которых каждая сторона понимает общую цель и свой вклад."),
            Translation("ky", "Өнөктөштүк", "Sabat жаңы өнөктөштүккө ачык", "Уюмдарды, эксперттерди жана активдүү адамдарды пайдалуу долбоорлорду чогуу түзүүгө чакырабыз.",
                "Өз ара урматтоо, жоопкерчилик жана коомдогу оң өзгөрүүлөр идеяларын бөлүшкөндөрдүн баары менен кызматташууга даярбыз.",
                "Өнөктөштөр билим берүү программаларына эксперт жана насаатчы катары катышып, практикалык долбоорлорду колдой алышат.",
                "Биз үчүн ар бир тарап жалпы максатты жана өз салымын түшүнгөн узак мөөнөттүү мамилелер өзгөчө маанилүү."),
            Translation("en", "Partnerships", "Sabat is open to new partnerships", "We invite organisations, experts and active citizens to create useful projects together.",
                "We welcome collaboration with everyone who shares the values of mutual respect, responsibility and positive change in society.",
                "Partners can contribute to educational programmes as experts and mentors, support practical projects or propose new directions for working together.",
                "Long-term relationships matter most to us: each side should understand the shared goal and its contribution."))
    ];

    private static NewsArticle Article(
        string slug,
        string category,
        DateTimeOffset publishedAt,
        params NewsTranslation[] translations) => new()
    {
        Id = Guid.NewGuid(),
        Slug = slug,
        Category = category,
        Status = NewsStatus.Published,
        PublishedAt = publishedAt,
        CreatedAt = publishedAt,
        UpdatedAt = publishedAt,
        Translations = translations
    };

    private static NewsTranslation Translation(
        string language,
        string category,
        string title,
        string excerpt,
        params string[] paragraphs) => new()
    {
        Id = Guid.NewGuid(),
        Language = language,
        Category = category,
        Title = title,
        Excerpt = excerpt,
        Content = string.Join("\n\n", paragraphs)
    };
}
