function setCookie(cname, cvalue, exdays) {
    var d = new Date();
    d.setTime(d.getTime() + (exdays * 1000 * 60));
    var expires = "expires=" + d.toGMTString();
    document.cookie = cname + "=" + cvalue + "; " + expires;
}

function getCookie(cname) {
    var name = cname + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i].trim();
        if (c.indexOf(name) == 0) return c.substring(name.length, c.length);
    }
    return "";
}

function formatDate(dateStr) {
    var date = new Date(dateStr);
    var monthStr = (date.getMonth() + 1).toString();
    var dayStr = date.getDate().toString();
    return date.getFullYear().toString() + '-' + '00'.substr(0, 2 - monthStr.length) + monthStr + '-' + '00'.substr(0, 2 - dayStr.length) + dayStr;
}

function formatTime(dateStr) {
    var date = new Date(dateStr);
    const year = date.getFullYear()
    const month = date.getMonth() + 1
    const day = date.getDate()
    const hour = date.getHours()
    const minute = date.getMinutes()
    const second = date.getSeconds()

    return [hour, minute, second].map(formatNumber).join(':')

}

function formatNumber (n) {
    n = n.toString()
    return n[1] ? n : '0' + n
}

function getUrlParameter(para) {
    var urlArr = window.location.href.split('?');
    if (urlArr.length < 2) {
        return '';
    }
    var paraArr = urlArr[1].trim().split('&');
    var value = '';
    for (var i = 0; i < paraArr.length; i++) {
        var paraPair = paraArr[i].trim();
        if (paraPair.startsWith(para + '=')) {
            value = paraPair.replace(para + '=', '');
            break;
        }
    }
    return value.trim();

}

function convertStrToDate(dateStr) {
    dateStr = dateStr.split(' ')[0].trim();
    var dateStrArr = dateStr.split('-');
    if (dateStrArr[1].length < 2) {
        dateStrArr[1] = '0' + dateStrArr[1];
    }
    if (dateStrArr[2].length < 2) {
        dateStrArr[2] = '0' + dateStrArr[2];
    }
    return new Date(dateStrArr[0] + '-' + dateStrArr[1] + '-' + dateStrArr[2]);
}