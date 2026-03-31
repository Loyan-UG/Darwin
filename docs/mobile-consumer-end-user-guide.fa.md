# Consumer App User Guide

این سند راهنمای استفاده از اپلیکیشن `Consumer` است. نام pageها، sectionها و buttonها عمداً به انگلیسی نوشته شده‌اند تا دقیقاً با چیزی که کاربر در برنامه می‌بیند یکسان باشند.

## Login

صفحه `Login` برای ورود کاربر به حساب شخصی استفاده می‌شود.

- `Email`: آدرس ایمیل حساب کاربر است.
- `Password`: رمز عبور حساب است.
- `Login`: ورود به برنامه را انجام می‌دهد.
- `Register`: کاربر را به صفحه ساخت حساب جدید می‌برد.
- `Forgot password?`: کاربر را به صفحه بازیابی رمز عبور می‌برد.
- `Send activation email`: اگر حساب ساخته شده ولی ایمیل هنوز تأیید نشده باشد، درخواست ارسال دوباره ایمیل فعال‌سازی را ثبت می‌کند.
- `Legal`: کاربر را به صفحه لینک‌های حقوقی و قانونی می‌برد.

روش استفاده:

1. ایمیل و رمز عبور را وارد کنید.
2. روی `Login` بزنید.
3. اگر حساب هنوز فعال نشده بود، از `Send activation email` استفاده کنید.

## Register

صفحه `Register` برای ساخت حساب جدید است.

- `First name`: نام کاربر
- `Last name`: نام خانوادگی کاربر
- `Email`: ایمیل ورود
- `Password`: رمز عبور
- `Confirm new password`: تکرار رمز عبور
- `Terms` و `Privacy`: لینک مشاهده اسناد حقوقی مربوط
- `Register`: ثبت نهایی حساب
- `Legal`: ورود به صفحه کامل لینک‌های حقوقی

روش استفاده:

1. اطلاعات هویتی و ایمیل را وارد کنید.
2. رمز عبور را وارد و تکرار کنید.
3. تیک‌های حقوقی لازم را فعال کنید.
4. روی `Register` بزنید.

## Forgot password

صفحه `Forgot password` برای درخواست لینک بازیابی رمز عبور استفاده می‌شود.

- `Email`: ایمیلی که کاربر با آن وارد برنامه می‌شود
- `Send reset instructions`: درخواست ارسال راهنمای بازیابی رمز
- `Go to Reset password`: اگر کاربر token بازیابی را دارد، به صفحه تغییر رمز می‌رود

روش استفاده:

1. ایمیل حساب را وارد کنید.
2. روی `Send reset instructions` بزنید.
3. ایمیل را بررسی کنید و در صورت دریافت token، به `Reset password` بروید.

## Reset password

صفحه `Reset password` برای ثبت رمز جدید با استفاده از token بازیابی است.

- `Email`: ایمیل حساب
- `Token`: توکن بازیابی که از ایمیل دریافت شده
- `New password`: رمز جدید
- `Confirm new password`: تکرار رمز جدید
- `Reset password`: ثبت نهایی رمز جدید

روش استفاده:

1. token ارسالی در ایمیل را کپی کنید.
2. اطلاعات را کامل وارد کنید.
3. روی `Reset password` بزنید.

## Discover

صفحه `Discover` برای پیدا کردن businessها و بررسی برنامه وفاداری آن‌ها است.

- `My Businesses`: businessهایی که کاربر قبلاً به آن‌ها join شده است
- `Explore`: فهرست businessهای قابل بررسی و پیوستن
- business cardها: اطلاعات خلاصه هر business را نشان می‌دهند

روش استفاده:

1. برای دیدن عضویت‌های فعلی، `My Businesses` را باز کنید.
2. برای پیدا کردن business جدید، `Explore` را باز کنید.
3. روی business موردنظر بزنید تا `Business Detail` باز شود.

## Business Detail

صفحه `Business Detail` جزئیات یک business و وضعیت loyalty آن را نمایش می‌دهد.

- `Join Loyalty Program`: عضویت در برنامه loyalty آن business
- بخش summary: وضعیت عضویت، points و جزئیات اصلی را نشان می‌دهد

روش استفاده:

1. business را از `Discover` باز کنید.
2. اگر هنوز عضو نیستید، روی `Join Loyalty Program` بزنید.
3. بعد از عضویت، برای دریافت QR به صفحه `QR` بروید.

## QR

صفحه `QR` برای نمایش QR قابل استفاده در loyalty flow است.

- QR code: کدی که business scanner آن را می‌خواند
- business selector یا context: مشخص می‌کند QR مربوط به کدام business است

روش استفاده:

1. business مناسب را انتخاب کنید.
2. QR را به اپراتور business نشان دهید تا scan انجام شود.

## Rewards

صفحه `Rewards` برای مشاهده loyalty accountها، points، rewardها و history استفاده می‌شود.

- `Business`: انتخاب loyalty account مربوط به یک business
- `Open selected business QR`: مستقیماً QR همان business را باز می‌کند
- `Available rewards`: rewardهای موجود
- `Rewards history`: تاریخچه تغییرات points و rewardها

روش استفاده:

1. از `Business` حساب موردنظر را انتخاب کنید.
2. points و rewardهای فعال را بررسی کنید.
3. در صورت نیاز با `Open selected business QR` به صفحه QR همان business بروید.

## Feed

صفحه `Feed` برای دیدن updateها، promotionها و محتوای مرتبط با businessهای کاربر است.

- feed cardها: promotion یا updateهای جدید را نشان می‌دهند
- scope selectorها: مشخص می‌کنند محتوا برای یک business خاص یا همه businessها نمایش داده شود

روش استفاده:

1. فهرست updateها را مرور کنید.
2. در صورت نیاز scope را تغییر دهید تا محتوای مرتبط‌تر ببینید.

## Profile

صفحه `Profile` برای مشاهده و مدیریت اطلاعات حساب است.

- `Save profile`: ذخیره تغییرات اطلاعات شخصی
- `Manage addresses`: مدیریت آدرس‌ها
- `Manage preferences`: مدیریت preferenceها
- `Open customer details`: مشاهده customer context در صورت وجود
- `Sync push registration`: همگام‌سازی وضعیت notification

روش استفاده:

1. اطلاعات پروفایل را بررسی یا اصلاح کنید.
2. روی `Save profile` بزنید.
3. اگر نیاز به تغییر تنظیمات بیشتر بود، از actionهای پایین صفحه استفاده کنید.

## Settings

صفحه `Settings` نقطه ورود به تنظیمات و بخش‌های پشتیبان حساب است.

- `Profile`: ورود به پروفایل
- `Orders & invoices`: مشاهده بخش مربوط به سفارش‌ها و فاکتورها
- `Privacy & preferences`: مدیریت تنظیمات حریم خصوصی
- `Change password`: تغییر رمز عبور
- `Legal`: ورود به مجموعه لینک‌های حقوقی
- `Logout`: خروج از حساب

روش استفاده:

1. بخش موردنیاز را انتخاب کنید.
2. پس از پایان کار، در صورت نیاز از `Logout` برای خروج استفاده کنید.

## نکات مهم

- اگر `Login` کار نکرد، ابتدا ایمیل و رمز عبور را دوباره بررسی کنید.
- اگر عملیات loyalty به‌درستی باز نشد، از داخل `Rewards` یا `QR` business درست را دوباره انتخاب کنید.
- اگر پیام قانونی یا policy لازم بود، همیشه از `Legal` استفاده کنید چون همه لینک‌های رسمی آنجا جمع شده‌اند.
