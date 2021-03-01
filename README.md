# Instagram Backup Script

This script will backup your Instagram account to a GitHub repo and is designed to run via GitHub actions

Before starting, you will need to follow the setup steps on the [Facebook Developer Documentation](https://developers.facebook.com/docs/instagram-api) to setup an app, you can also look at this [Medium Article](https://dev.to/mddanishyusuf/get-instagram-feed-data-without-code-1df7) or this [YouTube Video](https://www.youtube.com/watch?v=UhAe_pY1_I8) on how to get an Instagram Token

Basically: 

1. Create a Facebook App on the Facebook Developer Site
2. Add "Instagram Basic Display" as a Product
3. Under App > Roles > Instagram Testers add your account as a tester
4. Accept the invite at the Instagram [Manage Access Page](https://www.instagram.com/accounts/manage_access/)
5. Under App > Instagram Basic Display > Basic Display add your account as a tester in the User Token Generator section

Once you've got the token, the application basically uses it to make requests to: and then follow the pagination to retreive all posts

```
https://graph.instagram.com/me/media?fields=id,caption,media_type,media_url,id&limit=100&access_token=ACCESS_TOKEN
```

Additionally, the Access token is required in either your environment variables or GitHub secrets (depending on where you're running this)

```
IG_ACCESS_TOKEN=XXXXXXXXXXXXXXXXXXXX
```