    var config = {
        apiKey: '36f80a25438108f6582700a3921c3a4ca25fa1a8',
    // test key below
    // apiKey: '592b99ebdf88c091dad9b556b6d8de236ac97687',
    product: 'PRO_MULTISITE',
    text: {
        title: 'This site uses cookies.',
        intro: 'Some of these cookies are essential, while others help us to improve your experience.',
        thirdPartyTitle: 'Warning: Some cookies require your attention',
        thirdPartyDescription: 'Please follow the link(s) below to opt out manually.',
        initialState: 'open',
        notifyTitle: 'Your choice of cookies on this site',
        notifyDescription: 'We use cookies to optimize the functionality of the site and give you the best possible browsing experience.',
        accept: 'Accept',
        settings: 'Settings',
        acceptSettings: 'I accept',
        reject: 'I do not accept',
        rejectSettings: 'I do not accept',
        acceptRecommended: 'Accept recommended settings',
        necessaryTitle: '',
        necessaryDescription: '',
        on: 'on',
        off: 'off',
        },
    
        thirdPartyCookies: [{ "name": "AddThis", "optOutLink": "https://www.addthis.com/privacy/opt-out" }],

    optionalCookies: [
    {
        name: 'geolocation',
    label: 'Geolocation',
    description: 'Geolocation cookies help us to improve your user experience by providing you with a default language base on your location.',
    recommendedState: true,
    cookies: ['_ga*', '_gid*'],
    onAccept: function () {

    },
    onRevoke: function () {

    }
                    },
    {
        name: 'analytics',
    label: 'Analytics',
    description: 'Analytical cookies help us to improve our website by collecting information such as traffic and reporting on its usage.',
    recommendedState: true,
    cookies: ['_ga*', '_gid*'],
    onAccept: function () {
        window.dataLayer = window.dataLayer || [];
    function gtag() {dataLayer.push(arguments); }
    gtag('js', new Date());

    gtag('config', 'G-X99JP75TP1');

                        },
    onRevoke: function () {
        // Disable Google Analytics
        console.log('disable ga');
    window['ga-disable-' + 'G-X99JP75TP1'] = true;
                            // End Google Analytics
                        }
                    },

    ],

    position: 'LEFT',
    theme: 'DARK',
    initialState: 'notify',
    acceptBackground: '#982931',
    branding: {
        fontColor: "#FFF",
    fontSizeTitle: "31px",
    fontSizeIntro: "19px",
    fontSizeHeaders: "26px",
    fontSize: "16px",
    backgroundColor: "#419299",
    toggleText: "#0C2944",
    toggleColor: "#982931",
    toggleBackground: "#ffffff",
    buttonIcon: null,
    buttonIconWidth: "64px",
    buttonIconHeight: "64px",
    removeIcon: false,
    removeAbout: false
                },
    excludedCountries: ["all"]
            };

    CookieControl.load(config);

