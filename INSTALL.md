# Makapi Release Installation

Makapi release packages are signed with a **self-signed certificate**.
Because of that, Windows will only allow installation after you trust the included certificate.

## 1. Unpack The Release ZIP

Download the release ZIP and extract it to a folder first.

The extracted folder should include (among other files):

- A `.cer` certificate file
- A `.msix` package file

## 2. Install The Included Certificate (Required)

1. Double-click the `.cer` file, then click **Install Certificate**.

2. Select **Local Machine** (requires admin), then click **Next**.

3. Choose **Place all certificates in the following store**, then click **Browse**.

4. Select **Trusted Root Certification Authorities**, click **OK**, then **Next**, then **Finish**.

5. Click **Yes** on the UAC/security prompt.

## 3. Install The App

After the certificate is installed, run the included `.msix` file.

Without installing the certificate first, the MSIX installation will fail or be blocked by Windows trust checks.
