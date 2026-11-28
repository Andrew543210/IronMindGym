using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IronMindGym.Localization;

// Services write their messages in English and use them as keys; a Ukrainian request gets these texts.
public static class ServerMessages
{
    private static readonly Dictionary<string, string> Ukrainian = new(StringComparer.Ordinal)
    {
        ["Invalid email or password."] = "Неправильна пошта або пароль.",
        ["A user with this email already exists."] = "Користувач із такою поштою вже існує.",
        ["This username is already taken."] = "Це ім'я користувача вже зайняте.",
        ["Email or username is already registered."] = "Ця пошта або ім'я користувача вже зареєстровані.",
        ["Current password is incorrect."] = "Поточний пароль неправильний.",
        ["Enter your email."] = "Введіть електронну пошту.",
        ["Enter a valid email address."] = "Введіть коректну адресу електронної пошти.",
        ["Emails are limited to 120 characters."] = "Адреса пошти — не довше 120 символів.",
        ["Enter your password."] = "Введіть пароль.",
        ["Enter a password."] = "Введіть пароль.",
        ["Enter your current password."] = "Введіть поточний пароль.",
        ["Enter a new password."] = "Введіть новий пароль.",
        ["Use a password of at least 10 characters."] = "Пароль має містити щонайменше 10 символів.",
        ["Passwords are limited to 128 characters."] = "Пароль — не довше 128 символів.",
        ["Repeat the password."] = "Повторіть пароль.",
        ["Passwords must match."] = "Паролі мають збігатися.",
        ["Enter a username."] = "Введіть ім'я користувача.",
        ["Use 2–50 letters, numbers, underscores, dots or hyphens."] = "Використовуйте 2–50 латинських літер, цифр, підкреслень, крапок або дефісів.",
        ["Age must be between 12 and 100."] = "Вік має бути від 12 до 100.",
        ["Time zone names are limited to 64 characters."] = "Назва часового поясу — не довше 64 символів.",
        ["Enter a time zone such as Europe/Kyiv, or leave it empty to use UTC."] = "Вкажіть часовий пояс, наприклад Europe/Kyiv, або залиште поле порожнім, щоб використовувати UTC.",
        ["Choose English or Ukrainian."] = "Оберіть англійську або українську.",

        ["Complete your profile first."] = "Спершу заповніть профіль.",
        ["Enter a weight between 30 and 250 kg."] = "Введіть вагу від 30 до 250 кг.",
        ["Enter a height between 120 and 230 cm."] = "Введіть зріст від 120 до 230 см.",
        ["The bench press must be between 0 and 500 kg."] = "Жим лежачи має бути від 0 до 500 кг.",
        ["The squat must be between 0 and 500 kg."] = "Присідання має бути від 0 до 500 кг.",
        ["The deadlift must be between 0 and 500 kg."] = "Станова тяга має бути від 0 до 500 кг.",
        ["Plan between 1 and 7 workouts a week."] = "Плануйте від 1 до 7 тренувань на тиждень.",
        ["Choose a valid training split."] = "Оберіть наявний тип спліту.",
        ["The calorie target must be between 500 and 10000 kcal."] = "Ціль калорій має бути від 500 до 10000 ккал.",
        ["The protein target must be between 1 and 1000 g."] = "Ціль білків має бути від 1 до 1000 г.",
        ["The fat target must be between 1 and 1000 g."] = "Ціль жирів має бути від 1 до 1000 г.",
        ["The carbohydrate target must be between 1 and 2000 g."] = "Ціль вуглеводів має бути від 1 до 2000 г.",
        ["Choose an energy equation: none, male or female."] = "Оберіть формулу енергії: без формули, чоловіча або жіноча.",
        ["Choose a date within the last five years, up to today."] = "Оберіть дату за останні п'ять років, не пізніше сьогодні.",
        ["Calories must be 0–10000 kcal."] = "Калорії мають бути в межах 0–10000 ккал.",
        ["Protein must be 0–500 g."] = "Білки мають бути в межах 0–500 г.",
        ["Fat must be 0–300 g."] = "Жири мають бути в межах 0–300 г.",
        ["Carbs must be 0–1000 g."] = "Вуглеводи мають бути в межах 0–1000 г.",

        ["Enter a goal."] = "Введіть ціль.",
        ["A goal must be 3–80 characters long."] = "Ціль має містити від 3 до 80 символів.",
        ["Choose a deadline between today and five years from now."] = "Оберіть термін від сьогодні до п'яти років наперед.",
        [$"Finish or remove an existing goal before adding more. You can have {GoalService.MaxActiveGoals} active goals."] =
            $"Завершіть або видаліть одну з цілей, перш ніж додавати нову. Активних цілей може бути до {GoalService.MaxActiveGoals}.",

        ["A workout must last between 1 and 600 minutes."] = "Тренування має тривати від 1 до 600 хвилин.",
        ["Rate the workout from 1 to 5."] = "Оцініть тренування від 1 до 5.",
        ["Reviews are limited to 800 characters."] = "Відгук — не довше 800 символів.",
        ["Add between 1 and 200 sets before saving."] = "Додайте від 1 до 200 підходів перед збереженням.",
        ["Choose a valid training day."] = "Оберіть наявний день спліту.",
        ["Choose valid exercises and set types."] = "Оберіть наявні вправи й типи підходів.",
        ["Choose a valid exercise."] = "Оберіть наявну вправу.",
        [$"Weight must be between 0 and {TrackingRules.MaxLoadKg} kg."] = $"Вага має бути від 0 до {TrackingRules.MaxLoadKg} кг.",
        [$"Reps must be between 1 and {TrackingRules.MaxReps}."] = $"Повторень має бути від 1 до {TrackingRules.MaxReps}.",
        [$"Duration must be between 1 and {TrackingRules.MaxDurationSeconds} seconds."] = $"Тривалість має бути від 1 до {TrackingRules.MaxDurationSeconds} секунд.",
        [$"Intensity must be between 1 and {TrackingRules.MaxIntensity}."] = $"Інтенсивність має бути від 1 до {TrackingRules.MaxIntensity}.",
        [$"Rest must be between 0 and {TrackingRules.MaxRestSeconds} seconds."] = $"Відпочинок має тривати від 0 до {TrackingRules.MaxRestSeconds} секунд.",
        ["Repetitions, duration and intensity must be whole numbers within their allowed range."] = "Повторення, тривалість та інтенсивність мають бути цілими числами в допустимих межах.",
        ["Enter the starting and the latest result."] = "Введіть початковий і останній результати.",
        ["Save initial results first."] = "Спершу збережіть початкові результати.",
        ["This workout is already saved."] = "Це тренування вже збережено.",
        ["The draft has no id."] = "Чернетка не має ідентифікатора.",
        ["Send the unfinished workout as a JSON object."] = "Надішліть незавершене тренування як JSON-об'єкт.",
        ["This workout is too large to keep on the server."] = "Це тренування завелике, щоб зберегти його на сервері.",
        ["The workout contains a character that cannot be stored."] = "Тренування містить символ, який неможливо зберегти.",
        ["Write a comment first."] = "Спершу напишіть коментар.",
        [$"Comments are limited to {WorkoutSocialService.MaxCommentLength} characters."] = $"Коментар — не довше {WorkoutSocialService.MaxCommentLength} символів.",

        ["Choose another registered user."] = "Оберіть іншого зареєстрованого користувача.",

        [InvalidInput] = "Дані запиту некоректні.",
        [UnverifiedRequest] = "Не вдалося перевірити запит. Оновіть сторінку й спробуйте ще раз.",
        ["A non-empty request body is required."] = "Запит не містить даних.",
    };

    public const string InvalidInput = "The input was not valid.";
    public const string UnverifiedRequest = "The request could not be verified. Reload the page and try again.";

    public static IReadOnlyCollection<string> English => Ukrainian.Keys;

    public static string Translate(string english) =>
        Localized.IsUkrainian && Ukrainian.TryGetValue(english, out var ukrainian) ? ukrainian : english;

    public static void TranslateErrors(ModelStateDictionary modelState)
    {
        foreach (var entry in modelState.Values)
        {
            var messages = entry.Errors
                .Select(e => Translate(string.IsNullOrEmpty(e.ErrorMessage) ? InvalidInput : e.ErrorMessage))
                .ToList();
            entry.Errors.Clear();
            foreach (var message in messages)
                entry.Errors.Add(message);
        }
    }
}
