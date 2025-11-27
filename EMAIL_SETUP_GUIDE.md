# Gmail SMTP Email Configuration Guide

## Email Notifications Implemented

The system now sends email notifications for:
1. **Booking Confirmation** - When a reservation is created (pending payment)
2. **Payment Confirmation** - When a payment is successfully completed via PayPal

## Gmail SMTP Setup Instructions

### Step 1: Enable 2-Factor Authentication on Your Gmail Account
1. Go to https://myaccount.google.com/security
2. Under "Signing in to Google", enable **2-Step Verification**
3. Follow the setup wizard to configure 2FA

### Step 2: Generate an App Password
1. Go to https://myaccount.google.com/apppasswords
2. In "Select app", choose **Mail**
3. In "Select device", choose **Windows Computer** (or Other)
4. Click **Generate**
5. Copy the 16-character password (it will look like: `xxxx xxxx xxxx xxxx`)

### Step 3: Update appsettings.json

Open `appsettings.json` and fill in the email configuration:

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SenderEmail": "your.email@gmail.com",  // ← Add your Gmail address here
  "SenderPassword": "xxxx xxxx xxxx xxxx",  // ← Add the App Password here (remove spaces)
  "SenderName": "ARS Airlines"
}
```

**Important Notes:**
- Use the **App Password**, NOT your regular Gmail password
- Remove the spaces from the App Password (e.g., `xxxx xxxx xxxx xxxx` becomes `xxxxxxxxxxxxxxxx`)
- The sender email must match the Gmail account that generated the App Password

### Step 4: Test Email Functionality

1. Run the application: `dotnet run`
2. Make a test booking
3. Check the console output for:
   - `[EMAIL SENT] To: user@example.com, Subject: Booking Confirmation - ARS Airlines`
4. Check the recipient's email inbox for the confirmation email

### For Development/Testing Without Real Email

If you don't want to set up Gmail credentials yet:
- Leave `SenderEmail` and `SenderPassword` empty in `appsettings.json`
- The system will skip sending emails but log to console
- You'll see: `[EMAIL NOT SENT] To: user@example.com, Subject: ...`

## Email Templates

### Booking Confirmation Email Includes:
- Reservation details (confirmation number, status, passengers, class)
- Flight itinerary (multi-leg or single flight)
- Seat assignments (if selected)
- Total amount due
- Next steps for payment

### Payment Confirmation Email Includes:
- Payment details (transaction ID, amount, date, method)
- Reservation details (confirmation number, status)
- Complete flight itinerary
- Boarding instructions

## Troubleshooting

### "Failed to send email" errors
1. **Check credentials** - Ensure SenderEmail and SenderPassword are correct
2. **Verify App Password** - Make sure you're using the App Password, not your Gmail password
3. **Check 2FA** - Ensure 2-Step Verification is enabled on your Gmail account
4. **Firewall** - Ensure port 587 is not blocked
5. **Less Secure Apps** - If using an old Gmail account, you may need to enable "Less secure app access"

### Emails not arriving
1. Check spam/junk folder
2. Verify recipient email address is valid
3. Check console logs for successful send confirmation
4. Some email providers may delay delivery

### Gmail daily sending limits
- Gmail has a limit of ~500 emails per day for free accounts
- For higher volume, consider using SendGrid, Mailgun, or Amazon SES

## Alternative Email Services

If you prefer not to use Gmail, you can easily switch to:

### SendGrid (100 emails/day free)
```json
"Email": {
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": "587",
  "SenderEmail": "your-verified-email@yourdomain.com",
  "SenderPassword": "YOUR_SENDGRID_API_KEY",
  "SenderName": "ARS Airlines"
}
```

### Mailgun (5,000 emails/month free)
```json
"Email": {
  "SmtpHost": "smtp.mailgun.org",
  "SmtpPort": "587",
  "SenderEmail": "postmaster@your-domain.mailgun.org",
  "SenderPassword": "YOUR_MAILGUN_SMTP_PASSWORD",
  "SenderName": "ARS Airlines"
}
```

## Security Best Practices

1. **Never commit credentials to Git** - Add `appsettings.json` to `.gitignore`
2. **Use environment variables** for production:
   ```bash
   $env:Email__SenderEmail = "your.email@gmail.com"
   $env:Email__SenderPassword = "your-app-password"
   ```
3. **Use Azure Key Vault** or similar for production secrets
4. **Rotate App Passwords** regularly for security

## Current Implementation

- Service: `GmailEmailService.cs` - Uses .NET `SmtpClient` for sending
- Interface: `IEmailService.cs` - Simple abstraction for email sending
- Injection: Registered as scoped service in `Program.cs`
- Controllers: `PaymentController` and `ReservationController` send emails
- Email format: Plain text (can be upgraded to HTML if needed)
