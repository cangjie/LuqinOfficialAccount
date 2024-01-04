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
        'url': '/api/LimitUp/GetLimitUpTwiceNew',
        'defaultCountDays': 15,
        'defaultSort': '筹码'
    },
    {
        'name': '二连板查看所属概念',
        'description': '🌞表示第二天为资金流入。🌟表示高开。',
        'url': '/api/LimitUp/GetLinitUpTwiceWithConcept',
        'defaultCountDays': 15,
        'defaultSort': '缩量'
    },
    {
        'name': '昨日二连板查看所属概念',
        'description': '',
        'url': '/api/LimitUp/GetLastDayLimitUpTwiceWithConcept',
        'defaultCountDays': 15,
        'defaultSort': '缩量 desc'
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
        'description': '📈 表示上涨过程是二连板；🛍表示顶线双剑鞘；🔥表示日线KDJ和MACD双金叉；💩表示KDJ顶背离，应该注意；👍表示没有任何信号，仅仅是统计用途。',
        'url': '/api/BigRise/GetKDJMACDForHours',
        'defaultCountDays': 15,
        'defaultSort': 'MACD'
    },
    ///LimitUpTwiceOverHighTwice
    {
        'name': '二连板后两天收于涨停之上',
        'description': '',
        'url': '/api/LimitUp/LimitUpTwiceOverHighTwice',
        'defaultCountDays': 5,
        'defaultSort': 'MACD'
    },
    {
        'name': '涨停后两天收于涨停之上',
        'description': '',
        'url': '/api/LimitUp/LimitUpOverHighTwice',
        'defaultCountDays': 5,
        'defaultSort': 'MACD'
    },
    //LimitUpTwiceSettleHighTwice
    {
        'name': '二连板后，第一天收于涨停之上，第二天收盘高于之前三天最高。',
        'description': '',
        'url': '/api/LimitUp/LimitUpTwiceSettleHighTwice',
        'defaultCountDays': 5,
        'defaultSort': 'MACD'
    },
    {
        'name': '二连板吸筹',
        'description': '',
        'url': '/api/LimitUp/AfterChipsIn',
        'defaultCountDays': 5,
        'defaultSort': ''
    },
    {
        'name': '二连板吸筹2',
        'description': '',
        'url': '/api/BigRise/AfterChipsIn',
        'defaultCountDays': 5,
        'defaultSort': '筹码'
    },
    {
        'name': '一板吸筹',
        'description': '',
        'url': '/api/BigRise/AfterChipsInLimitUpOnce',
        'defaultCountDays': 5,
        'defaultSort': '筹码'
    },
    //GetLinitUpVolumeReduceRise

    {
        'name': '一板后缩量暴涨',
        'description': '',
        'url': '/api/LimitUp/GetLinitUpVolumeReduceRise',
        'defaultCountDays': 5,
        'defaultSort': '缩量'
    },
    {
        'name': '二板后连续两不板收过前高',
        'description': '',
        'url': '/api/LimitUp/GetLimitUpTwiceAdjustOverHigh',
        'defaultCountDays': 5,
        'defaultSort': '缩量'
    },
    //GetLimitUpTwiceAdjustOverHighest
    {
        'name': '二板后连续两不板收过前高且创半年新高',
        'description': '',
        'url': '/api/LimitUp/GetLimitUpTwiceAdjustOverHighest',
        'defaultCountDays': 15,
        'defaultSort': '缩量'
    },
    //GetLimitUpAdjustSettleOverHighestAndLimitUpAgain
    {
        'name': '反包',
        'description': '涨停后调整收到涨停之上不超过三天再次涨停',
        'url': '/api/LimitUp/GetLimitUpAdjustSettleOverHighestAndLimitUpAgain',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '涨停后调整收到涨停之上不超过三天再次涨停，并且最后一天涨停和之前一天同量。',
        'description': '',
        'url': '/api/LimitUp/GetLimitUpAdjustSettleOverHighestAndLimitUpAgainVolumeEqual',
        'defaultCountDays': 15,
        'defaultSort': '缩量'
    },
    {
        'name': '涨停后调整收到涨停之上不超过三天再次涨停，并且最后一天涨停比之前一天放量。',
        'description': '',
        'url': '/api/LimitUp/GetLimitUpAdjustSettleOverHighestAndLimitUpAgainVolumeHigh',
        'defaultCountDays': 15,
        'defaultSort': '缩量'
    },
    {
        'name': '涨停后调整收到涨停之上不超过三天再次涨停',
        'description': '',
        'url': '/api/LimitUp/GetLimitUpAdjustSettleOverHighestAndLimitUpAgainOpenHigh',
        'defaultCountDays': 15,
        'defaultSort': '缩量'
    },
    {
        'name': '三阴不破阳',
        'description': '',
        'url': '/api/LimitUp/LimitUp3Green',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '底部放量吸筹',
        'description': '底部阳线放量，然后缩量调整，再次放量阳线买入。',
        'url': '/api/DoubleVolume/GetVolumeDoubleAgain',
        'defaultCountDays': 15,
        'defaultSort': '放量'
    },
    {
        'name': '底部放量吸筹后缩量调整',
        'description': '底部阳线放量，然后缩量调整，再次放量阳线后缩量调整买入。',
        'url': '/api/DoubleVolume/GetVolumeDoubleAgainGreenVolumeReduce',
        'defaultCountDays': 15,
        'defaultSort': '放量'
    },
    {
        'name': '周线倍量柱后，日线找买点20日均线',
        'description': '底部阳线放量，然后缩量调整，再次放量阳线后缩量调整买入。',
        'url': '/api/DoubleVolume/GetVolumeDoubleWeekTouchLine20',
        'defaultCountDays': 15,
        'defaultSort': '放量 desc'
    },
    {
        'name': '周线倍量柱后，日线找买点10日均线',
        'description': '底部阳线放量，然后缩量调整，再次放量阳线后缩量调整买入。',
        'url': '/api/DoubleVolume/GetVolumeDoubleWeekTouchLine10',
        'defaultCountDays': 15,
        'defaultSort': '放量 desc'
    },
    {
        'name': '周线倍量柱后，日线找买点3线',
        'description': '底部阳线放量，然后缩量调整，再次放量阳线后缩量调整买入。',
        'url': '/api/DoubleVolume/GetVolumeDoubleWeekTouchLine33',
        'defaultCountDays': 15,
        'defaultSort': '放量 desc'
    },
    //LowRiseRateWithDoubleVolume
    {
        'name': '小涨幅后，放倍量',
        'description': '等待二连板',
        'url': '/api/BigRise/LowRiseRateWithDoubleVolume',
        'defaultCountDays': 15,
        'defaultSort': '放量 desc'
    },
    {
        'name': '九转线后放量',
        'description': '20均线持续向上',
        'url': '/api/DoubleVolume/WithDemark',
        'defaultCountDays': 15,
        'defaultSort': '筹码 desc'
    },
    {
        'name': '调整后再度涨停高开',
        'description': '',
        'url': '/api/LimitUp/AjustOpenHigh',
        'defaultCountDays': 15,
        'defaultSort': '代码 desc'
    },
    {
        'name': '三连板，第三板盘中跌幅超6%',
        'description': '',
        'url': '/api/LimitUp/LimitUpTripleLongLeg',
        'defaultCountDays': 15,
        'defaultSort': '代码 desc'
    },
    {
        'name': '三连板，第三板盘中跌幅超5%',
        'description': '',
        'url': '/api/LimitUp/LimitUpTripleLongFoot',
        'defaultCountDays': 15,
        'defaultSort': '代码 desc'
    },
    {
        'name': '反包后，1%以上的高开且盘中触及上日涨停收盘',
        'description': '',
        'url': '/api/ResultCache/GetLimitUpAdjustSettleOverHighestAndLimitUpAgain',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    //ReverseLimitUpNoChanceTomorrow
    {
        'name': '反包后，第一天没有机会看第二天 ',
        'description': '反包后，1%以上的高开且盘中未触及上日涨停收盘，然后看第二天高开同样条件的机会。',
        'url': '/api/ResultCache/ReverseLimitUpNoChanceTomorrow',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '1，反包之后高开但是没有回踩，当日涨停;  2, 次日回踩前日涨停，并进场',
        'description': '缺口',
        'url': '/api/LimitUp/LimitUpAdjustLimitUpWithChance',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    //ReverseOpenHigBiggreenAndOpenStandard
    {
        'name': '高开下跌后平开看涨停概率',
        'description': '',
        'url': '/api/LimitUp/ReverseOpenHighBiggreenAndOpenStandard',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包后创新高然后跌停二连板',
        'description': '',
        'url': '/api/LimitUp/ReverseSettleHighWithLimitDownTwice',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包后高开立刻买入',
        'description': '',
        'url': '/api/LimitUp/ReverseOpenHigh',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '二连板反包',
        'description': '',
        'url': '/api/LimitUp/ReverseWithLimitUpTwice',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包',
        'description': '📈表示第三天涨停；🔥表示创新高的反包；🌞表示第二天为资金流入 🌟表示高开',
        'url': '/api/LimitUp/Reverse',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包高开创新高',
        'description': '',
        'url': '/api/Reverse/OpenOverHigh',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包高开创新高 以前一日收盘价买入',
        'description': '',
        'url': '/api/Reverse/OpenOverHighBackToLastSettle',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': 'T字板反包',
        'description': '',
        'url': '/api/Reverse/WithT',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包马头高开',
        'description': '',
        'url': '/api/Reverse/HorseHead',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包高开',
        'description': '3⃣️ 高开3%以内；3⃣️🥉高开3%以内且涨停；2⃣️高开3%-6%；2⃣️🥈高开3%-6%且涨停；1⃣️高开6%-9%；1⃣️🥇高开6%-9%且涨停；0⃣️高开大于9%；0⃣️🐮高开大于9%且涨停',
        'url': '/api/Reverse/OpenHigh',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包高开，当日亏损，计算未来五日平进平出几率。',
        'description': '',
        'url': '/api/Reverse/ResumeSettleLossRate',
        'defaultCountDays': 5,
        'defaultSort': '代码'
    },
    {
        'name': '反包高开，回踩收于涨停上下1%。',
        'description': '',
        'url': '/api/Reverse/OpenHighGoBack',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '三连板，第三板回踩第一板收盘价之上的2%，第四日买入。',
        'description': '',
        'url': '/api/LimitUp/TripleWithLongLeg',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包，碰三线',
        'description': '',
        'url': '/api/Reverse/Touch3Line',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包—高开回踩，当日未涨停',
        'description': '',
        'url': '/api/Reverse/OpenHighNoLimitUp',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    //LimitUpOpenHighGoBack
    {
        'name': '二连板高开回踩涨停',
        'description': '二连板高开回踩，但是最终不能跌破前一天涨停。当天跌破则止损。',
        'url': '/api/LimitUp/LimitUpOpenHighGoBack',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包当日跌停',
        'description': '',
        'url': '/api/Reverse/OpenHighLimitDown',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {

        'name': '反包后双马头不涨停',
        'description': '资金流出价格扛住连续两天做标记',
        'url': '/api/Reverse/DoubleHorse',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {

        'name': '反包后单马头不涨停',
        'description': '资金流出价格扛住做标记',
        'url': '/api/Reverse/SingleHorse',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {



        'name': '反包高开严选',
        'description': '',
        'url': '/api/Reverse/OpenHighCollection',
        'defaultCountDays': 15,
        'defaultSort': '代码'
        
    },
    {
        'name': '反包调整两个马头以内',
        'description': '',
        'url': '/api/Reverse/HorseHeadLess2',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '一月内首次二连板后买入',
        'description': '',
        'url': '/api/LimitUp/GreenBeforeFirstLimitUpTwice',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包低开涨停后统计再次涨停的概率',
        'description': '',
        'url': '/api/Reverse/OpenLowLimitUp',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '资金持续流出，价格均线多头排列',
        'description': '📈表示调整过程中有涨停板',
        'url': '/api/BakDaily/GetContinousFlowout',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {

        'name': '一板后，资金持续流入',
        'description': '',
        'url': '/api/BakDaily/LimitUpInflow',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {
        'name': '二连板后，资金持续流入，统计资金流入期间，需要有涨停板。',
        'description': '📈最近5日有涨停板',
        'url': '/api/BakDaily/LimitUpTwiceInflow',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {
        'name': '一板一马头，资金流入',
        'description': '📈资金流出，价格扛住',
        'url': '/api/BakDaily/LimitUpWithSingleHorse',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {
        'name': '一板二马头，资金流入',
        'description': '📈资金流出，价格扛住',
        'url': '/api/BakDaily/LimitUpWithDoubleHorse',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {
        'name': '二板一马头，资金流入',
        'description': '📈资金流出，价格扛住',
        'url': '/api/BakDaily/LimitUpTwiceWithSingleHorse',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {
        'name': '二板二马头，资金流入',
        'description': '📈资金流出，价格扛住',
        'url': '/api/BakDaily/LimitUpTwiceWithDoubleHorse',
        'defaultCountDays': 15,
        'defaultSort': '流入 desc'
    },
    {
        'name': '反包后，阴线下跌',
        'description': '📉表示跌停',
        'url': '/api/Reverse/OpenHighWithBigGreen',
        'defaultCountDays': 15,
        'defaultSort': '代码 desc'
    },
    {
        'name': '反包后调整过程中资金出入',
        'description': '经过测试，没有意义',
        'url': '/api/Reverse/ViewAdjust',
        'defaultCountDays': 15,
        'defaultSort': '均换手'
    },
    {
        'name': '突破',
        'description': '经过测试，没有意义',
        'url': '/api/BakDaily/BreakOut',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '二连板后大阴',
        'description': '经过测试，没有意义',
        'url': '/api/LimitUp/BigGreenAfterLimitupTwice',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '反包低开高走',
        'description': '第一天低开，收于反包涨停之上；第二天，无论高低开，依然收于涨停之上，收盘买',
        'url': '/api/Reverse/OpenLowSettleHighTwice',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '二连板低开高走',
        'description': '第一天低开，收于反包涨停之上；第二天，无论高低开，依然收于涨停之上，收盘买',
        'url': '/api/LimitUp/OpenLowSettleHighTwice',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '大涨幅后，收复60日均线',
        'description': '',
        'url': '/api/BigRise/BreakMa60',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    },
    {
        'name': '大涨幅后，收复30日均线',
        'description': '',
        'url': '/api/BigRise/BreakMa30',
        'defaultCountDays': 15,
        'defaultSort': '代码'
    }
];
