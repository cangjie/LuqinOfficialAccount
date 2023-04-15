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
    {
        'name': '等待红绿灯',
        'description': '',
        'url': '/api/LimitUp/GetLimitUpAdjust',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
    {
        'name': 'MACD低位金叉，且均线多头排列，收20日均线上。',
        'description': '',
        'url': '/api/MACD/MACDGoldForkLow',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
    {
        'name': 'KDJ超卖金叉，20日均线趋势向上。',
        'description': '',
        'url': '/api/KDJ/GetOverSell',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
    {
        'name': 'KDJ超卖金叉，之前数日在3线上',
        'description': '',
        'url': '/api/KDJ/GetAbove3Line',
        'defaultCountDays': 15,
        'defaultSort': 'MACD日'
    },
    {
        'name': 'KDJ周线金叉后，小时线找买点',
        'description': '',
        'url': '/api/KDJ/HourAfterWeek',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
    {
        'name': '二连板，双剑鞘。',
        'description': '',
        'url': '/api/LimitUp/LimitUpTwiceSwordTwice',
        'defaultCountDays': 5,
        'defaultSort': 'MACD'
    },
    {
        'name': '二连板，KDJ超卖。',
        'description': '',
        'url': '/api/LimitUp/LimitUpTwiceOverSell',
        'defaultCountDays': 15,
        'defaultSort': 'MACD'
    },
    {
        'name': '大涨幅回落后，小时KDJ超卖且MACD共振',
        'description': '📈 表示上涨过程是二连板；🛍表示顶线双剑鞘。',
        'url': '/api/BigRise/GetKDJMACDForHours',
        'defaultCountDays': 15,
        'defaultSort': 'MACD'
    }
    ///api/KDJ/HourAfterWeek
];
