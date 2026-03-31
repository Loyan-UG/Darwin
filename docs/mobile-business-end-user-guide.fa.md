# Business App User Guide

این سند راهنمای استفاده از اپلیکیشن `Business` است. نام screenها، sectionها و buttonها همان نام انگلیسی داخل برنامه هستند تا آموزش کاربر و پشتیبانی دقیق‌تر انجام شود.

## Login

صفحه `Login` برای ورود اپراتور business به برنامه استفاده می‌شود.

- `Email`: ایمیل حساب اپراتور
- `Password`: رمز عبور
- `Sign in`: ورود به برنامه
- `Accept invitation`: ورود به flow پذیرش دعوت‌نامه برای اپراتورهای جدید
- `Send activation email`: ارسال دوباره ایمیل فعال‌سازی اگر حساب ساخته شده ولی هنوز فعال نشده باشد
- `Legal`: ورود به صفحه لینک‌های حقوقی

روش استفاده:

1. ایمیل و رمز عبور را وارد کنید.
2. روی `Sign in` بزنید.
3. اگر کاربر جدید هستید و دعوت‌نامه دارید، از `Accept invitation` استفاده کنید.

## Accept invitation

صفحه `Accept invitation` برای اپراتورهایی است که از طرف business دعوت شده‌اند.

- `Invitation token`: توکن دعوت
- `Preview invitation`: بررسی اطلاعات دعوت
- `Accept and continue`: پذیرش دعوت و ادامه ورود

روش استفاده:

1. توکن دعوت را وارد کنید.
2. روی `Preview invitation` بزنید.
3. اگر اطلاعات درست بود، با `Accept and continue` ادامه دهید.

## Home

صفحه `Home` نمای کلی business و وضعیت عملیاتی را نشان می‌دهد.

- بخش summary: وضعیت business، plan و context اصلی را نمایش می‌دهد
- بخش status: هشدارها یا محدودیت‌های عملیاتی را نشان می‌دهد

روش استفاده:

1. پس از ورود، ابتدا وضعیت کلی را در `Home` بررسی کنید.
2. اگر هشدار عملیاتی وجود داشت، قبل از scan یا reward management آن را برطرف کنید.

## Dashboard

صفحه `Dashboard` برای KPI و گزارش‌های عملیاتی استفاده می‌شود.

- `Refresh`: بارگذاری مجدد داده‌ها
- `Export CSV`: خروجی CSV برای گزارش
- `Export PDF`: خروجی PDF برای گزارش
- بخش metricها: sessionها، accrualها، redemptionها و شاخص‌های دیگر را نمایش می‌دهد

روش استفاده:

1. برای بررسی عملکرد اخیر، `Dashboard` را باز کنید.
2. در صورت نیاز به اشتراک گزارش، از `Export CSV` یا `Export PDF` استفاده کنید.

## Scan

صفحه `Scan` برای شروع اسکن QR مشتری استفاده می‌شود.

- `Scan`: شروع فرآیند اسکن
- `Last token`: آخرین QR خوانده‌شده
- بخش status: نشان می‌دهد اپراتور چه permissionهایی برای accrual یا redemption دارد

روش استفاده:

1. قبل از شروع، وضعیت readiness و permissionها را بررسی کنید.
2. روی `Scan` بزنید.
3. QR مشتری را اسکن کنید.
4. بعد از اسکن، برنامه وارد `Session` می‌شود تا نتیجه همان‌جا پردازش شود.

## Session

صفحه `Session` نقطه اصلی بررسی و تأیید scan است.

- `Confirm Accrual`: ثبت افزایش points
- `Confirm Redemption`: ثبت redeem شدن reward یا کسر points
- بخش customer/session detail: اطلاعات scan جاری را نشان می‌دهد

روش استفاده:

1. بعد از اسکن، جزئیات مشتری و session را بررسی کنید.
2. بسته به سناریو، `Confirm Accrual` یا `Confirm Redemption` را بزنید.

## Rewards

صفحه `Rewards` برای مدیریت reward tierها و campaignها استفاده می‌شود.

### Reward editor

- `Refresh`: بارگذاری مجدد configuration
- `New tier`: شروع ساخت reward tier جدید
- `Create tier` یا `Update tier`: ذخیره tier
- `Delete tier`: حذف tier انتخاب‌شده
- `Current tiers`: فهرست tierهای فعلی

روش استفاده:

1. برای ساخت tier جدید، اطلاعات را در editor وارد کنید و `Create tier` را بزنید.
2. برای ویرایش، یکی از آیتم‌های `Current tiers` را انتخاب کنید.
3. بعد از انتخاب، اطلاعات در editor بارگذاری می‌شود و می‌توانید `Update tier` یا `Delete tier` را استفاده کنید.

### Campaign editor

- `Create campaign` یا `Update campaign`: ذخیره campaign
- `New campaign`: پاک‌کردن editor و شروع draft جدید
- `Campaigns`: فهرست campaignها
- `Clear filters`: پاک‌کردن filterهای campaign list
- preset buttonها مثل `JoinedMembers`, `TierSegment`, `PointsThreshold`, `DateWindow`: اعمال preset برای targeting

روش استفاده:

1. اطلاعات campaign را در editor وارد کنید.
2. در صورت نیاز از presetها برای targeting استفاده کنید.
3. برای ذخیره، `Create campaign` یا `Update campaign` را بزنید.
4. برای مرور campaignهای موجود، به بخش `Campaigns` نگاه کنید.

## Settings

صفحه `Settings` برای تنظیمات حساب اپراتور و بخش‌های پشتیبان است.

- `Profile`: مشاهده و ویرایش پروفایل
- `Change password`: تغییر رمز عبور
- `Staff access badge`: مشاهده badge داخلی
- `Subscription`: بررسی وضعیت subscription
- `Legal`: لینک‌های حقوقی
- `Logout`: خروج از حساب
- `Delete account`: حذف حساب در صورت فعال بودن این امکان

روش استفاده:

1. وارد `Settings` شوید.
2. بخش موردنیاز را انتخاب کنید.
3. برای خروج امن از حساب، از `Logout` استفاده کنید.

## Staff access badge

صفحه `Staff access badge` برای badge داخلی و موقت اپراتور استفاده می‌شود.

- `Refresh badge`: تولید یا به‌روزرسانی badge

روش استفاده:

1. badge را فقط برای فرایندهای داخلی business استفاده کنید.
2. در صورت انقضا، از `Refresh badge` استفاده کنید.

## Subscription

صفحه `Subscription` وضعیت plan و subscription business را نشان می‌دهد.

- `Refresh subscription status`: بارگذاری مجدد وضعیت
- `Open Loyan website`: انتقال به سایت برای مدیریت billing و plan

روش استفاده:

1. وضعیت فعلی plan را بررسی کنید.
2. اگر نیاز به تغییر plan یا billing بود، از `Open Loyan website` استفاده کنید.

## Profile

صفحه `Profile` برای مدیریت اطلاعات اپراتور است.

- `Save profile`: ذخیره تغییرات

روش استفاده:

1. اطلاعات لازم را اصلاح کنید.
2. روی `Save profile` بزنید.

## نکات مهم

- اگر `Scan` غیرفعال بود، معمولاً یا permission کافی ندارید یا business در وضعیت عملیاتی مناسب نیست.
- همه عملیات پس از scan باید در `Session` نهایی شوند و نه در خود `Scan`.
- برای مسائل حقوقی یا policy همیشه از `Legal` استفاده کنید تا کاربر به لینک رسمی هدایت شود.
