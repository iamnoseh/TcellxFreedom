using Microsoft.Extensions.Logging;
using TcellxFreedom.Domain.Entities;
using TcellxFreedom.Domain.Enums;
using TcellxFreedom.Domain.Interfaces;

namespace TcellxFreedom.Infrastructure.Data.Seeders;

public sealed class TcellPassSeeder(
    IPassTaskTemplateRepository templateRepository,
    ILevelRewardRepository rewardRepository,
    ILogger<TcellPassSeeder> logger)
{
    public async Task SeedTaskTemplatesAsync(CancellationToken ct = default)
    {
        if (await templateRepository.AnyExistsAsync(ct)) return;

        var templates = new List<(int day, string title, string desc, int xp, TaskCategory cat, bool premium, int sort)>
        {
            // День 1
            (1, "Проверьте баланс", "Откройте приложение MyTcell и перейдите на главную страницу, чтобы проверить баланс.", 15, TaskCategory.Balance, false, 1),
            (1, "Измените язык приложения", "Перейдите в настройки профиля и измените язык приложения на таджикский или русский.", 20, TaskCategory.Settings, false, 2),
            (1, "Найдите название тарифа", "Откройте раздел «Тарифный план» и узнайте название вашего текущего тарифа.", 15, TaskCategory.Tariff, false, 3),

            // День 2
            (2, "Конвертер трафика: 200 МБ → Минуты", "Перейдите в раздел конвертера трафика и узнайте, сколько минут составляет 200 МБ.", 20, TaskCategory.Traffic, false, 1),
            (2, "Пополните счёт на 1 сомони", "Пополните свой счёт на 1 сомони любым доступным способом.", 25, TaskCategory.Balance, false, 2),
            (2, "Посмотрите историю платежей", "Проверьте платежи за последнюю неделю в разделе «История».", 30, TaskCategory.Payment, true, 3),

            // День 3
            (3, "Узнайте цену мобильного интернета", "Откройте раздел «Мобильный интернет» и узнайте стоимость пакета 1 ГБ.", 20, TaskCategory.Tariff, false, 1),
            (3, "Прочитайте условия Викторины", "Перейдите в меню «Викторины» и ознакомьтесь с условиями «Меломания».", 25, TaskCategory.General, false, 2),
            (3, "Проверьте расходы на интернет за 3 дня", "Проверьте сумму, потраченную на интернет за последние 3 дня.", 30, TaskCategory.Payment, true, 3),

            // День 4
            (4, "Посмотрите последние новости", "Перейдите в раздел «Новости» и прочитайте последнюю экономическую новость.", 15, TaskCategory.General, false, 1),
            (4, "Проверьте цену сервиса Музыка", "Перейдите в раздел «Музыка» и узнайте стоимость услуги «Симфония».", 20, TaskCategory.Tariff, false, 2),
            (4, "Позвоните 5 минут и проверьте баланс", "Позвоните кому-нибудь на 5 минут, затем проверьте баланс.", 25, TaskCategory.Balance, false, 3),

            // День 5
            (5, "Конвертируйте 500 МБ в СМС", "В разделе Конвертер проверьте, сколько СМС составляет 500 МБ.", 20, TaskCategory.Traffic, false, 1),
            (5, "Найдите раздел «Кино и ТВ»", "Найдите раздел «Кино и ТВ» и просмотрите список каналов.", 20, TaskCategory.General, false, 2),
            (5, "Включите уведомления", "Перейдите в настройки приложения и включите уведомления.", 15, TaskCategory.Settings, false, 3),
            (5, "Цена Роуминга (Премиум)", "Откройте раздел «Роуминг» и проверьте стоимость интернета за рубежом.", 35, TaskCategory.Tariff, true, 4),

            // День 6
            (6, "Найдите цену «СМС Викторина»", "В разделе Викторины найдите стоимость «СМС Викторина».", 20, TaskCategory.Tariff, false, 1),
            (6, "Найдите пакет «Доп. пакет 15 ГБ»", "Найдите пакет «Доп. пакет 15 ГБ» и проверьте срок акции.", 25, TaskCategory.Tariff, false, 2),
            (6, "Найдите код PUK (Премиум)", "Найдите свой PUK-код в настройках профиля.", 30, TaskCategory.Settings, true, 3),

            // День 7
            (7, "Сравните два тарифа", "В разделе «Все тарифы» сравните два разных тарифа.", 25, TaskCategory.Tariff, false, 1),
            (7, "Посмотрите новости о футболе Таджикистана", "В разделе «Новости» найдите статью о футболе Таджикистана.", 15, TaskCategory.General, false, 2),
            (7, "Прочитайте раздел «Часто задаваемые вопросы»", "Откройте раздел FAQ и прочитайте его.", 15, TaskCategory.General, false, 3),

            // День 8
            (8, "Найдите сервис «Караоке»", "В разделе «Музыка» найдите услугу «Караоке» и узнайте её стоимость.", 20, TaskCategory.Tariff, false, 1),
            (8, "Проверьте цену пакета 25 ГБ", "Перейдите в раздел «Интернет» и найдите цену самого большого пакета 25 ГБ.", 25, TaskCategory.Tariff, false, 2),
            (8, "Порекомендуйте приложение другу (Премиум)", "Порекомендуйте приложение MyTcell другу или члену семьи.", 15, TaskCategory.General, true, 3),

            // День 9
            (9, "Проверьте историю конвертаций", "Проверьте историю конвертаций в нижней части страницы Конвертера.", 20, TaskCategory.Traffic, false, 1),
            (9, "Найдите раздел «Образование»", "Найдите раздел «Образование» и ознакомьтесь с его услугами.", 20, TaskCategory.General, false, 2),
            (9, "Прочитайте условия «Доверительного платежа» (Премиум)", "Найдите раздел «Доверительный платёж» и ознакомьтесь с условиями.", 30, TaskCategory.Payment, true, 3),

            // День 10
            (10, "Смените пароль или включите FaceID", "Перейдите в настройки безопасности и смените пароль входа или включите FaceID.", 30, TaskCategory.Settings, false, 1),
            (10, "Привяжите карту в Кошельке", "Откройте раздел «Кошелёк» и узнайте, как привязать карту.", 25, TaskCategory.Payment, false, 2),
            (10, "Выйдите и войдите снова (Премиум)", "Выйдите из приложения и снова войдите с новыми данными.", 20, TaskCategory.Settings, true, 3),

            // День 11
            (11, "Найдите акцию «В 2 раза больше»", "В разделе «Мобильный интернет» найдите значок акции «В 2 раза больше».", 25, TaskCategory.Tariff, false, 1),
            (11, "Посмотрите пакеты минут", "Найдите раздел «Минуты» и просмотрите доступные пакеты звонков.", 20, TaskCategory.Tariff, false, 2),
            (11, "Проверьте остаток минут", "Проверьте остаток минут звонков на главной странице.", 15, TaskCategory.Balance, false, 3),

            // День 12
            (12, "Найдите цену игры «Мореплаватель»", "В разделе Викторины узнайте, сколько стоит «Игра Мореплаватель».", 25, TaskCategory.General, false, 1),
            (12, "Посмотрите последние «Происшествия»", "В разделе «Новости» найдите последнюю новость из рубрики «Происшествия».", 20, TaskCategory.General, false, 2),
            (12, "Проверьте скорость интернета (Премиум)", "Проверьте скорость интернета, открыв любой сайт.", 15, TaskCategory.Traffic, true, 3),

            // День 13
            (13, "Переключите конвертер в «СМС → Минуты»", "В Конвертере переключите показатель на «СМС → Минуты» и узнайте курс.", 20, TaskCategory.Traffic, false, 1),
            (13, "Посмотрите пакеты СМС", "В разделе услуг найдите и просмотрите пакеты СМС.", 20, TaskCategory.Tariff, false, 2),
            (13, "Проверьте расходы на СМС за неделю (Премиум)", "Проверьте сумму, потраченную на СМС за последнюю неделю.", 25, TaskCategory.Payment, true, 3),

            // День 14
            (14, "Проверьте цену роуминга за рубежом", "Откройте раздел «Роуминг» и узнайте стоимость интернета за границей.", 25, TaskCategory.Tariff, false, 1),
            (14, "Прочитайте информацию о «Симфония»", "В разделе «Музыка» прочитайте информацию о сервисе «Симфония».", 20, TaskCategory.Tariff, false, 2),
            (14, "Переведите 1 сомони другу (Премиум)", "Переведите 1 сомони с баланса на номер друга.", 30, TaskCategory.Balance, true, 3),

            // День 15
            (15, "Найдите TezNet", "Найдите раздел «TezNet» и ознакомьтесь с условиями домашнего интернета.", 25, TaskCategory.Tariff, false, 1),
            (15, "Проверьте цену пакета 5 ГБ", "В разделе «Интернет» найдите цену пакета 5 ГБ.", 20, TaskCategory.Tariff, false, 2),
            (15, "Найдите номер технической поддержки", "Найдите номер технической поддержки внутри приложения.", 15, TaskCategory.General, false, 3),

            // День 16
            (16, "Добавьте тариф в «Избранное»", "Добавьте понравившийся тариф в раздел «Избранное».", 20, TaskCategory.Tariff, false, 1),
            (16, "Найдите информацию о «USSD Викторина»", "В разделе Викторины узнайте, как выиграть до 2000 сомони в «USSD Викторина».", 25, TaskCategory.General, false, 2),
            (16, "Добавьте электронную почту (Премиум)", "Откройте настройки профиля и добавьте свой адрес электронной почты.", 20, TaskCategory.Settings, true, 3),

            // День 17
            (17, "Найдите экономические новости", "В разделе «Новости» найдите и прочитайте статью из рубрики «Экономика».", 25, TaskCategory.General, false, 1),
            (17, "Конвертируйте 400 МБ в минуты", "В Конвертере узнайте, сколько минут составляет 400 МБ.", 20, TaskCategory.Traffic, false, 2),
            (17, "Проверьте остаток мегабайт (Премиум)", "Проверьте остаток мегабайт интернета на главной странице.", 15, TaskCategory.Balance, true, 3),

            // День 18
            (18, "Проверьте список активных услуг", "Перейдите в раздел «Услуги» и просмотрите список своих активных услуг.", 20, TaskCategory.General, false, 1),
            (18, "Включите/отключите одну из бесплатных услуг", "Включите или отключите одну из бесплатных услуг.", 25, TaskCategory.General, false, 2),
            (18, "Посмотрите цены золотых номеров (Премиум)", "Найдите раздел «Смена номера» и узнайте стоимость золотых номеров.", 30, TaskCategory.Tariff, true, 3),

            // День 19
            (19, "Проверьте коммунальные платежи в Кошельке", "Откройте раздел «Кошелёк» и узнайте, можно ли оплатить электричество или воду.", 25, TaskCategory.Payment, false, 1),
            (19, "Проверьте комиссию при оплате картой", "Узнайте, какая комиссия берётся при оплате услуг с карты.", 20, TaskCategory.Payment, false, 2),
            (19, "Сделайте скриншот баланса (Премиум)", "Сделайте скриншот баланса для сохранения.", 15, TaskCategory.Balance, true, 3),

            // День 20
            (20, "Просмотрите все разделы", "Ещё раз просмотрите все разделы из скриншотов (Интернет, Музыка, Викторина, Новости).", 30, TaskCategory.General, false, 1),
            (20, "Найдите самую важную новость дня", "Прочитайте раздел «Новости» и найдите самую важную новость сегодня.", 20, TaskCategory.General, false, 2),
            (20, "Оцените приложение", "Напишите своё мнение о приложении в разделе «Оценить».", 25, TaskCategory.General, false, 3),
            (20, "Подведите итоги вашего TcellPass (Премиум)", "Подумайте обо всём, что вы узнали о приложении MyTcell за 20 дней.", 35, TaskCategory.General, true, 4),
        };

        var entities = templates.Select(t =>
            PassTaskTemplate.Create(t.day, t.title, t.desc, t.xp, t.cat, t.premium, t.sort))
            .ToList();

        await templateRepository.AddRangeAsync(entities, ct);
        logger.LogInformation("TcellPass: {Count} шаблонов задач добавлено в базу данных.", entities.Count);
    }

    public async Task SeedLevelRewardsAsync(CancellationToken ct = default)
    {
        if (await rewardRepository.AnyExistsAsync(ct)) return;

        var rewards = new List<LevelReward>
        {
            // Уровень 1
            LevelReward.Create(1, UserTier.Free, RewardType.MB, "100 МБ Интернета", 100),
            LevelReward.Create(1, UserTier.Premium, RewardType.MB, "500 МБ Интернета", 500),
            // Уровень 2
            LevelReward.Create(2, UserTier.Free, RewardType.Minutes, "10 Минут", 10),
            LevelReward.Create(2, UserTier.Premium, RewardType.Minutes, "50 Минут", 50),
            // Уровень 3
            LevelReward.Create(3, UserTier.Free, RewardType.MB, "200 МБ Интернета", 200),
            LevelReward.Create(3, UserTier.Premium, RewardType.GB, "1 ГБ Интернета", 1),
            // Уровень 4
            LevelReward.Create(4, UserTier.Free, RewardType.Minutes, "15 Минут", 15),
            LevelReward.Create(4, UserTier.Premium, RewardType.Minutes, "100 Минут", 100),
            // Уровень 5
            LevelReward.Create(5, UserTier.Free, RewardType.MB, "500 МБ Интернета", 500),
            LevelReward.Create(5, UserTier.Premium, RewardType.GB, "3 ГБ Интернета", 3),
            // Уровень 6
            LevelReward.Create(6, UserTier.Free, RewardType.Minutes, "20 Минут", 20),
            LevelReward.Create(6, UserTier.Premium, RewardType.Minutes, "150 Минут", 150),
            // Уровень 7
            LevelReward.Create(7, UserTier.Free, RewardType.SMS, "10 СМС", 10),
            LevelReward.Create(7, UserTier.Premium, RewardType.SMS, "100 СМС", 100),
            // Уровень 8
            LevelReward.Create(8, UserTier.Free, RewardType.MB, "700 МБ Интернета", 700),
            LevelReward.Create(8, UserTier.Premium, RewardType.GB, "5 ГБ Интернета", 5),
            // Уровень 9
            LevelReward.Create(9, UserTier.Free, RewardType.Minutes, "25 Минут", 25),
            LevelReward.Create(9, UserTier.Premium, RewardType.Minutes, "200 Минут", 200),
            // Уровень 10
            LevelReward.Create(10, UserTier.Free, RewardType.Badge, "Значок «Активный»", null),
            LevelReward.Create(10, UserTier.Premium, RewardType.VipStatus, "VIP-статус (Золотой)", null),
            // Уровень 11
            LevelReward.Create(11, UserTier.Free, RewardType.GB, "1 ГБ Интернета", 1),
            LevelReward.Create(11, UserTier.Premium, RewardType.GB, "7 ГБ Интернета", 7),
            // Уровень 12
            LevelReward.Create(12, UserTier.Free, RewardType.Minutes, "30 Минут", 30),
            LevelReward.Create(12, UserTier.Premium, RewardType.Minutes, "300 Минут", 300),
            // Уровень 13
            LevelReward.Create(13, UserTier.Free, RewardType.MB, "1.5 ГБ Интернета", 1500),
            LevelReward.Create(13, UserTier.Premium, RewardType.GB, "10 ГБ Интернета", 10),
            // Уровень 14
            LevelReward.Create(14, UserTier.Free, RewardType.SMS, "50 СМС", 50),
            LevelReward.Create(14, UserTier.Premium, RewardType.SMS, "500 СМС", 500),
            // Уровень 15
            LevelReward.Create(15, UserTier.Free, RewardType.Minutes, "40 Минут", 40),
            LevelReward.Create(15, UserTier.Premium, RewardType.Service, "Безлимитные звонки (1 день)", null),
            // Уровень 16
            LevelReward.Create(16, UserTier.Free, RewardType.GB, "2 ГБ Интернета", 2),
            LevelReward.Create(16, UserTier.Premium, RewardType.GB, "15 ГБ Интернета", 15),
            // Уровень 17
            LevelReward.Create(17, UserTier.Free, RewardType.Service, "Симфония (1 день)", null),
            LevelReward.Create(17, UserTier.Premium, RewardType.Service, "Караоке (1 неделя)", null),
            // Уровень 18
            LevelReward.Create(18, UserTier.Free, RewardType.Minutes, "50 Минут", 50),
            LevelReward.Create(18, UserTier.Premium, RewardType.Minutes, "500 Минут", 500),
            // Уровень 19
            LevelReward.Create(19, UserTier.Free, RewardType.GB, "3 ГБ Интернета", 3),
            LevelReward.Create(19, UserTier.Premium, RewardType.GB, "25 ГБ Интернета", 25),
            // Уровень 20
            LevelReward.Create(20, UserTier.Free, RewardType.Bonus, "Маленький сюрприз (Бонус)", null),
            LevelReward.Create(20, UserTier.Premium, RewardType.Raffle, "Смартфон Samsung (Шанс на победу)", null),
        };

        await rewardRepository.AddRangeAsync(rewards, ct);
        logger.LogInformation("TcellPass: {Count} наград уровней добавлено в базу данных.", rewards.Count);
    }
}
