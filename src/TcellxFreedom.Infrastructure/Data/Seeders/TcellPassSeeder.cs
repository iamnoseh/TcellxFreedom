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
            // Рӯзи 1
            (1, "Балансро санҷед", "Замимаи MyTcell-ро кушоед ва ба саҳифаи асосӣ рафта балансатонро бинед.", 15, TaskCategory.Balance, false, 1),
            (1, "Забони замимаро иваз кунед", "Ба танзимоти профил рафта забони замимаро ба тоҷикӣ ё русӣ иваз кунед.", 20, TaskCategory.Settings, false, 2),
            (1, "Номи тарифатонро пайдо кунед", "Бахши «Нақшаи тарифӣ»-ро кушоед ва номи тарифи ҷории худро бинед.", 15, TaskCategory.Tariff, false, 3),

            // Рӯзи 2
            (2, "Конвертери трафик: 200 МБ → Дақиқа", "Ба бахши Конвертери трафик равед ва бубинед, ки 200 МБ ба чанд дақиқа баробар аст.", 20, TaskCategory.Traffic, false, 1),
            (2, "1 сомонӣ ба ҳисоб илова кунед", "Бо ягон усули мавҷуда 1 сомонӣ ба ҳисоби худ илова кунед.", 25, TaskCategory.Balance, false, 2),
            (2, "Таърихи пардохтро бинед", "Пардохтҳои ҳафтаи охирро дар бахши «Таърих» санҷед.", 30, TaskCategory.Payment, true, 3),

            // Рӯзи 3
            (3, "Нархи интернети мобилиро бинед", "Бахши «Интернети мобилӣ»-ро кушоед ва нархи бастаи 1 ГБ-ро бинед.", 20, TaskCategory.Tariff, false, 1),
            (3, "Шартҳои Викторинаро хонед", "Ба менюи «Викторинаҳо» равед ва шартҳои «Меломания»-ро хонед.", 25, TaskCategory.General, false, 2),
            (3, "Хароҷоти интернети 3 рӯзаро санҷед", "Маблағи сарфшуда барои интернетро дар 3 рӯзи охир тафтиш кунед.", 30, TaskCategory.Payment, true, 3),

            // Рӯзи 4
            (4, "Ахбори охиринро бинед", "Ба бахши «Ахбор» равед ва хабари охирини иқтисодиро хонед.", 15, TaskCategory.General, false, 1),
            (4, "Нархи хизмати Мусиқиро санҷед", "Ба бахши «Мусиқӣ» равед ва нархи хизматрасонии «Симфония»-ро бинед.", 20, TaskCategory.Tariff, false, 2),
            (4, "5 дақиқа занг зада балансро санҷед", "Ба касе занг зада 5 дақиқа сӯҳбат кунед, сипас балансатонро санҷед.", 25, TaskCategory.Balance, false, 3),

            // Рӯзи 5
            (5, "500 МБ-ро ба СМС конвертатсия кунед", "Дар бахши Конвертер санҷед, ки 500 МБ ба чанд СМС иваз мешавад.", 20, TaskCategory.Traffic, false, 1),
            (5, "Бахши «Кино ва ТВ»-ро пайдо кунед", "Бахши «Кино ва ТВ»-ро ёфта, рӯйхати каналҳоро аз назар гузаронед.", 20, TaskCategory.General, false, 2),
            (5, "Огоҳиномаҳоро фаъол кунед", "Ба танзимоти замима равед ва огоҳиномаҳоро фаъол созед.", 15, TaskCategory.Settings, false, 3),
            (5, "Нархи Роуминг (Премиум)", "Бахши «Роуминг»-ро кушоед ва нархи интернетро дар хориҷа санҷед.", 35, TaskCategory.Tariff, true, 4),

            // Рӯзи 6
            (6, "Нархи «СМС Викторина»-ро пайдо кунед", "Дар бахши Викторинаҳо нархи «СМС Викторина»-ро пайдо кунед.", 20, TaskCategory.Tariff, false, 1),
            (6, "Бастаи «Доп. пакет 15 ГБ»-ро ёбед", "Бастаи «Доп. пакет 15 ГБ»-ро ёфта, муҳлати аксияи онро санҷед.", 25, TaskCategory.Tariff, false, 2),
            (6, "Рамзи PUK-ро пайдо кунед (Премиум)", "Рамзи PUK-и худро дар танзимоти профил пайдо кунед.", 30, TaskCategory.Settings, true, 3),

            // Рӯзи 7
            (7, "Ду тарифро муқоиса кунед", "Дар бахши «Ҳамаи тарифҳо» ду тарифи гуногунро бо ҳам муқоиса кунед.", 25, TaskCategory.Tariff, false, 1),
            (7, "Ахбори футболи Тоҷикистонро бинед", "Дар бахши «Ахбор» мақолае дар бораи футболи Тоҷикистон пайдо кунед.", 15, TaskCategory.General, false, 2),
            (7, "Бахши «Тез-тез додашаванда»-ро хонед", "Бахши FAQ (Тез-тез додашаванда)-ро кушоед ва хонед.", 15, TaskCategory.General, false, 3),

            // Рӯзи 8
            (8, "Хизмати «Караоке»-ро пайдо кунед", "Дар бахши «Мусиқӣ» хизматрасонии «Караоке»-ро пайдо карда нархашро бинед.", 20, TaskCategory.Tariff, false, 1),
            (8, "Нархи бастаи 25 ГБ-ро санҷед", "Ба бахши «Интернет» рафта, нархи калонтарин бастаи 25 ГБ-ро пайдо кунед.", 25, TaskCategory.Tariff, false, 2),
            (8, "Замимаро ба дӯст тавсия диҳед (Премиум)", "Замимаи MyTcell-ро ба яке аз дӯстон ё аъзоёни оилаатон тавсия диҳед.", 15, TaskCategory.General, true, 3),

            // Рӯзи 9
            (9, "Таърихи конвертатсияро санҷед", "Таърихи конвертатсияҳои худро дар поёни саҳифаи Конвертер тафтиш кунед.", 20, TaskCategory.Traffic, false, 1),
            (9, "Бахши «Маориф»-ро пайдо кунед", "Бахши «Маориф»-ро ёфта, хизматрасониҳои онро аз назар гузаронед.", 20, TaskCategory.General, false, 2),
            (9, "Шартҳои «Пардохти эътимодӣ»-ро хонед (Премиум)", "Бахши «Пардохти эътимодӣ»-ро ёфта, шартҳои онро хонед.", 30, TaskCategory.Payment, true, 3),

            // Рӯзи 10
            (10, "Рамзро иваз кунед ё FaceID-ро фаъол созед", "Ба танзимоти амният равед ва рамзи воридшавиро иваз кунед ё FaceID-ро фаъол созед.", 30, TaskCategory.Settings, false, 1),
            (10, "Картро дар Кошелёк пайваст кунед", "Бахши «Кошелёк»-ро кушоед ва тарзи пайваст кардани картро бинед.", 25, TaskCategory.Payment, false, 2),
            (10, "Аз замима баромада, дубора ворид шавед (Премиум)", "Аз замима баромада, бо маълумоти нав дубора ворид шавед.", 20, TaskCategory.Settings, true, 3),

            // Рӯзи 11
            (11, "Аксияи «2 баробар бештар» -ро пайдо кунед", "Дар бахши «Интернети мобилӣ» нишони аксияи «2 баробар бештар»-ро ёбед.", 25, TaskCategory.Tariff, false, 1),
            (11, "Бастаҳои дақиқаҳоро бинед", "Бахши «Дақиқаҳо»-ро ёфта, бастаҳои мавҷудаи зангро аз назар гузаронед.", 20, TaskCategory.Tariff, false, 2),
            (11, "Бақияи дақиқаҳоро санҷед", "Бақияи дақиқаҳои зангиатонро дар саҳифаи асосӣ санҷед.", 15, TaskCategory.Balance, false, 3),

            // Рӯзи 12
            (12, "Нархи бозии «Мореплаватель»-ро пайдо кунед", "Дар бахши Викторинаҳо санҷед, ки барои «Игра Мореплаватель» чӣ қадар лозим аст.", 25, TaskCategory.General, false, 1),
            (12, "Ахбори охирини «Ҳодисаҳо»-ро бинед", "Дар бахши «Ахбор» хабари охирини «Ҳодисаҳо»-ро пайдо кунед.", 20, TaskCategory.General, false, 2),
            (12, "Суръати интернетатонро санҷед (Премиум)", "Бо кушодани ягон сомона суръати интернетатонро санҷед.", 15, TaskCategory.Traffic, true, 3),

            // Рӯзи 13
            (13, "Конвертерро ба «СМС → Дақиқа» гузоред", "Дар Конвертер нишондиҳандаро ба «СМС → Дақиқа» гузоред ва қурбашро бинед.", 20, TaskCategory.Traffic, false, 1),
            (13, "Бастаҳои СМС-ро бинед", "Дар бахши хизматрасониҳо бастаҳои СМС-ро пайдо карда, онҳоро аз назар гузаронед.", 20, TaskCategory.Tariff, false, 2),
            (13, "Хароҷоти СМС-и ҳафтаро санҷед (Премиум)", "Маблағи сарфшуда барои СМС-ро дар як ҳафтаи охир санҷед.", 25, TaskCategory.Payment, true, 3),

            // Рӯзи 14
            (14, "Нархи Роуминг дар хориҷаро санҷед", "Бахши «Роуминг»-ро кушоед ва нархи интернетро дар хориҷи кишвар бинед.", 25, TaskCategory.Tariff, false, 1),
            (14, "Маълумоти «Симфония»-ро хонед", "Дар бахши «Мусиқӣ» маълумотро дар бораи «Симфония» хонед.", 20, TaskCategory.Tariff, false, 2),
            (14, "1 сомонӣ ба дӯсте интиқол диҳед (Премиум)", "Аз балансатон 1 сомонӣ ба рақами дӯстатон интиқол диҳед.", 30, TaskCategory.Balance, true, 3),

            // Рӯзи 15
            (15, "TezNet-ро пайдо кунед", "Бахши «TezNet»-ро ёфта, шартҳои интернети хонагиро бинед.", 25, TaskCategory.Tariff, false, 1),
            (15, "Нархи бастаи 5 ГБ-ро санҷед", "Дар бахши «Интернет» нархи бастаи 5 ГБ-ро пайдо кунед.", 20, TaskCategory.Tariff, false, 2),
            (15, "Рақами дастгирии техникиро пайдо кунед", "Рақами дастгирии техникиро дар дохили замима пайдо кунед.", 15, TaskCategory.General, false, 3),

            // Рӯзи 16
            (16, "Тарифро ба «Дӯстдоштаҳо» илова кунед", "Тарифе, ки ба шумо писанд аст, ба бахши «Дӯстдоштаҳо» (Избранное) илова кунед.", 20, TaskCategory.Tariff, false, 1),
            (16, "Маълумоти «USSD Викторина»-ро пайдо кунед", "Дар бахши Викторинаҳо бубинед, ки барои «USSD Викторина» то 2000 сомонӣ чӣ тавр бурдан мумкин аст.", 25, TaskCategory.General, false, 2),
            (16, "Почтаи электронии худро илова кунед (Премиум)", "Танзимоти профилро кушоед ва почтаи электронии худро илова кунед.", 20, TaskCategory.Settings, true, 3),

            // Рӯзи 17
            (17, "Ахбори иқтисодиро пайдо кунед", "Дар бахши «Ахбор» мақолае аз рукни «Иқтисод»-ро пайдо карда хонед.", 25, TaskCategory.General, false, 1),
            (17, "400 МБ-ро ба дақиқа конвертатсия кунед", "Дар Конвертер санҷед, ки 400 МБ ба чанд дақиқа баробар мешавад.", 20, TaskCategory.Traffic, false, 2),
            (17, "Бақияи мегабайтҳоро санҷед (Премиум)", "Бақияи мегабайтҳои интернетатонро дар саҳифаи асосӣ санҷед.", 15, TaskCategory.Balance, true, 3),

            // Рӯзи 18
            (18, "Рӯйхати хизматрасониҳои фаъолро санҷед", "Ба бахши «Хизматрасониҳо» равед ва рӯйхати хизматҳои фаъолшудаи худро тафтиш кунед.", 20, TaskCategory.General, false, 1),
            (18, "Яке аз хизматҳои бепулро фаъол/хомӯш кунед", "Яке аз хизматрасониҳои бепулро фаъол ё хомӯш созед.", 25, TaskCategory.General, false, 2),
            (18, "Нархи рақамҳои тиллоиро бинед (Премиум)", "Бахши «Иваз намудани рақам»-ро пайдо карда, нархи рақамҳои тиллоиро санҷед.", 30, TaskCategory.Tariff, true, 3),

            // Рӯзи 19
            (19, "Дар Кошелёк пардохти коммуналиро санҷед", "Бахши «Кошелёк»-ро кушоед ва бубинед, ки пардохти барқ ё об мавҷуд аст.", 25, TaskCategory.Payment, false, 1),
            (19, "Комиссияи пардохти картро санҷед", "Санҷед, ки барои пардохти хизматрасониҳо аз корт чӣ қадар комиссия мегирад.", 20, TaskCategory.Payment, false, 2),
            (19, "Скриншоти балансатонро гиред (Премиум)", "Скриншоти балансатонро барои хотира гиред.", 15, TaskCategory.Balance, true, 3),

            // Рӯзи 20
            (20, "Ҳамаи бахшҳоро аз назар гузаронед", "Ҳамаи бахшҳои дар скриншотҳо буда (Интернет, Мусиқӣ, Викторина, Ахбор)-ро бори дигар баррасӣ кунед.", 30, TaskCategory.General, false, 1),
            (20, "Ахбори муҳимтарини рӯзро пайдо кунед", "Бахши «Ахбор»-ро хонда, хабари муҳимтарини имрӯзро пайдо кунед.", 20, TaskCategory.General, false, 2),
            (20, "Замимаро баҳо диҳед", "Фикри худро дар бораи замима дар бахши «Баҳо диҳед»-ро нависед.", 25, TaskCategory.General, false, 3),
            (20, "Сафари TcellPass-атонро хулоса кунед (Премиум)", "Дар бораи ҳама чизе ки дар 20 рӯз аз замимаи MyTcell омӯхтед, фикр кунед.", 35, TaskCategory.General, true, 4),
        };

        var entities = templates.Select(t =>
            PassTaskTemplate.Create(t.day, t.title, t.desc, t.xp, t.cat, t.premium, t.sort))
            .ToList();

        await templateRepository.AddRangeAsync(entities, ct);
        logger.LogInformation("TcellPass: {Count} шаблони вазифа ба пойгоҳ илова карда шуд.", entities.Count);
    }

    public async Task SeedLevelRewardsAsync(CancellationToken ct = default)
    {
        if (await rewardRepository.AnyExistsAsync(ct)) return;

        var rewards = new List<LevelReward>
        {
            // Дараҷа 1
            LevelReward.Create(1, UserTier.Free, RewardType.MB, "100 МБ Интернет", 100),
            LevelReward.Create(1, UserTier.Premium, RewardType.MB, "500 МБ Интернет", 500),
            // Дараҷа 2
            LevelReward.Create(2, UserTier.Free, RewardType.Minutes, "10 Дақиқа", 10),
            LevelReward.Create(2, UserTier.Premium, RewardType.Minutes, "50 Дақиқа", 50),
            // Дараҷа 3
            LevelReward.Create(3, UserTier.Free, RewardType.MB, "200 МБ Интернет", 200),
            LevelReward.Create(3, UserTier.Premium, RewardType.GB, "1 ГБ Интернет", 1),
            // Дараҷа 4
            LevelReward.Create(4, UserTier.Free, RewardType.Minutes, "15 Дақиқа", 15),
            LevelReward.Create(4, UserTier.Premium, RewardType.Minutes, "100 Дақиқа", 100),
            // Дараҷа 5
            LevelReward.Create(5, UserTier.Free, RewardType.MB, "500 МБ Интернет", 500),
            LevelReward.Create(5, UserTier.Premium, RewardType.GB, "3 ГБ Интернет", 3),
            // Дараҷа 6
            LevelReward.Create(6, UserTier.Free, RewardType.Minutes, "20 Дақиқа", 20),
            LevelReward.Create(6, UserTier.Premium, RewardType.Minutes, "150 Дақиқа", 150),
            // Дараҷа 7
            LevelReward.Create(7, UserTier.Free, RewardType.SMS, "10 СМС", 10),
            LevelReward.Create(7, UserTier.Premium, RewardType.SMS, "100 СМС", 100),
            // Дараҷа 8
            LevelReward.Create(8, UserTier.Free, RewardType.MB, "700 МБ Интернет", 700),
            LevelReward.Create(8, UserTier.Premium, RewardType.GB, "5 ГБ Интернет", 5),
            // Дараҷа 9
            LevelReward.Create(9, UserTier.Free, RewardType.Minutes, "25 Дақиқа", 25),
            LevelReward.Create(9, UserTier.Premium, RewardType.Minutes, "200 Дақиқа", 200),
            // Дараҷа 10
            LevelReward.Create(10, UserTier.Free, RewardType.Badge, "Нишони «Фаъол»", null),
            LevelReward.Create(10, UserTier.Premium, RewardType.VipStatus, "Статуси VIP (Тиллоӣ)", null),
            // Дараҷа 11
            LevelReward.Create(11, UserTier.Free, RewardType.GB, "1 ГБ Интернет", 1),
            LevelReward.Create(11, UserTier.Premium, RewardType.GB, "7 ГБ Интернет", 7),
            // Дараҷа 12
            LevelReward.Create(12, UserTier.Free, RewardType.Minutes, "30 Дақиқа", 30),
            LevelReward.Create(12, UserTier.Premium, RewardType.Minutes, "300 Дақиқа", 300),
            // Дараҷа 13
            LevelReward.Create(13, UserTier.Free, RewardType.MB, "1.5 ГБ Интернет", 1500),
            LevelReward.Create(13, UserTier.Premium, RewardType.GB, "10 ГБ Интернет", 10),
            // Дараҷа 14
            LevelReward.Create(14, UserTier.Free, RewardType.SMS, "50 СМС", 50),
            LevelReward.Create(14, UserTier.Premium, RewardType.SMS, "500 СМС", 500),
            // Дараҷа 15
            LevelReward.Create(15, UserTier.Free, RewardType.Minutes, "40 Дақиқа", 40),
            LevelReward.Create(15, UserTier.Premium, RewardType.Service, "Зангҳои бемаҳдуд (1 рӯз)", null),
            // Дараҷа 16
            LevelReward.Create(16, UserTier.Free, RewardType.GB, "2 ГБ Интернет", 2),
            LevelReward.Create(16, UserTier.Premium, RewardType.GB, "15 ГБ Интернет", 15),
            // Дараҷа 17
            LevelReward.Create(17, UserTier.Free, RewardType.Service, "Симфония (1 рӯз)", null),
            LevelReward.Create(17, UserTier.Premium, RewardType.Service, "Караоке (1 ҳафта)", null),
            // Дараҷа 18
            LevelReward.Create(18, UserTier.Free, RewardType.Minutes, "50 Дақиқа", 50),
            LevelReward.Create(18, UserTier.Premium, RewardType.Minutes, "500 Дақиқа", 500),
            // Дараҷа 19
            LevelReward.Create(19, UserTier.Free, RewardType.GB, "3 ГБ Интернет", 3),
            LevelReward.Create(19, UserTier.Premium, RewardType.GB, "25 ГБ Интернет", 25),
            // Дараҷа 20
            LevelReward.Create(20, UserTier.Free, RewardType.Bonus, "Сурпризи хурд (Бонус)", null),
            LevelReward.Create(20, UserTier.Premium, RewardType.Raffle, "Смартфони Samsung (Шанси бурд)", null),
        };

        await rewardRepository.AddRangeAsync(rewards, ct);
        logger.LogInformation("TcellPass: {Count} ҷоизаи дараҷаҳо ба пойгоҳ илова карда шуд.", rewards.Count);
    }
}
