/* ============ ENGLISH / ಕನ್ನಡ ============ */
// Elements with data-i18n="key" switch to I18N.kn[key] when ಕನ್ನಡ is chosen.
const I18N = {
  kn: {
    tagline: 'ಭಾರತವನ್ನು ಮುನ್ನಡೆಸುತ್ತೇವೆ. ನಂಬಿಕೆ ತಲುಪಿಸುತ್ತೇವೆ.',
    'nav.vehicles': 'ವಾಹನಗಳು',
    'nav.how': 'ಹೇಗೆ ಕೆಲಸ ಮಾಡುತ್ತದೆ',
    'nav.partner': 'ಪಾಲುದಾರರಾಗಿ',
    'nav.login': 'ಲಾಗಿನ್',
    'cta.book': 'ಲಾರಿ ಬುಕ್ ಮಾಡಿ',
    'cta.register': 'ನಿಮ್ಮ ವಾಹನ ನೋಂದಾಯಿಸಿ',
    'hero.l1': 'ಏನನ್ನಾದರೂ ಸಾಗಿಸಿ.',
    'hero.l2': 'ಎಲ್ಲಿಗಾದರೂ.',
    'hero.l3': 'ಭಾರತದಾದ್ಯಂತ.',
    'hero.sub':
      'ಪರಿಶೀಲಿತ ಲಾರಿ ಮಾಲೀಕರು, ವೃತ್ತಿಪರ ಚಾಲಕರು ಮತ್ತು ಸ್ಮಾರ್ಟ್ ಲಾಜಿಸ್ಟಿಕ್ಸ್ ತಂತ್ರಜ್ಞಾನದ ಬಲದಿಂದ ನಂಬಲರ್ಹ ಸಾರಿಗೆ.',
    'hero.p1': 'ಪರಿಶೀಲಿತ ಮಾಲೀಕರು ಮತ್ತು ಚಾಲಕರು',
    'hero.p2': 'ಪಿಕಪ್ ಮತ್ತು ಡೆಲಿವರಿಗೆ OTP',
    'q.title': 'ತಕ್ಷಣ ದರ ಪಡೆಯಿರಿ',
    'q.pick': 'ಪಿಕಪ್ ಸ್ಥಳ',
    'q.drop': 'ಡೆಲಿವರಿ ಸ್ಥಳ',
    'q.weight': 'ಅಂದಾಜು ತೂಕ',
    'q.vehicle': 'ವಾಹನದ ಪ್ರಕಾರ',
    'q.cta': 'ಸಾರಿಗೆ ದರ ಪಡೆಯಿರಿ',
    'veh.h': 'ಸರಿಯಾದ ವಾಹನವನ್ನು ಆಯ್ಕೆಮಾಡಿ',
    'how.h': 'ವಿಚಾರಣೆಯಿಂದ ಡೆಲಿವರಿವರೆಗೆ ಎಂಟು ಹಂತಗಳು.',
    'safe.h': 'ಸುರಕ್ಷಿತ ಸಾರಿಗೆ. ಪರಿಶೀಲಿತ ಜಾಲ.',
    'own.h': 'ಲಾರಿ ಇದೆಯೇ? ನಮ್ಮೊಂದಿಗೆ ನಿಮ್ಮ ವ್ಯಾಪಾರ ಬೆಳೆಸಿ.',
  },
};

const LANG_KEY = 'procargo.site.lang';

function setLanguage(lang) {
  document.documentElement.lang = lang;
  document.querySelectorAll('[data-i18n]').forEach((element) => {
    element.dataset.en ??= element.textContent;
    const translated = lang === 'kn' ? I18N.kn[element.dataset.i18n] : null;
    element.textContent = translated ?? element.dataset.en;
  });
  document.querySelectorAll('[data-lang]').forEach((button) => {
    button.setAttribute('aria-pressed', String(button.dataset.lang === lang));
  });
  try {
    localStorage.setItem(LANG_KEY, lang);
  } catch {
    /* the choice just isn't remembered */
  }
}

function initLanguage() {
  document.querySelectorAll('[data-lang]').forEach((button) => {
    button.addEventListener('click', () => setLanguage(button.dataset.lang));
  });
  let saved = 'en';
  try {
    saved = localStorage.getItem(LANG_KEY) || 'en';
  } catch {
    /* default to English */
  }
  if (saved !== 'en') setLanguage(saved);
}
