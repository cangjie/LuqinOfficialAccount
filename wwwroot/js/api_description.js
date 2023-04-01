var apiDescriptionList = [
    {
        'name': '大涨幅后回调，KD首次金叉',
        'description': '之前有超过30%以上的的涨幅，KD金叉后选出。主要看筹码集中度低于15%，而且下跌到横盘过程中，筹码集中度收窄的。📈 表示筹码分布小于15%，并且MACD绝对值小于0.5.',
        'url': '/api/BigRise/GetKDJForDays',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
    {
        'name': '突破50 100关口',
        'description': '',
        'url': '/api/Stock/BreakGate',
        'defaultCountDays': 15,
        'defaultSort': ''
    },
    {
        'name': '二连板',
        'description': '筹码集中，MACD低  📈 表示筹码分布小于15%，并且MACD值小于1.🛍️表示KDJ出现超买，同时具有📈和🛍️则标记为🔥。',
        'url': '/api/LimitUp/GetLimitUpTwice',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
];
