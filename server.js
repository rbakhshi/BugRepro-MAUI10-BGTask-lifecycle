function GetFormattedDate(date) {
  var month = ("0" + (date.getMonth() + 1)).slice(-2);
  var day = ("0" + (date.getDate())).slice(-2);
  var year = date.getFullYear();
  var hour = ("0" + (date.getHours())).slice(-2);
  var min = ("0" + (date.getMinutes())).slice(-2);
  var seg = ("0" + (date.getSeconds())).slice(-2);
  var mil = ("0" + (date.getMilliseconds())).slice(-3);
  return year + "-" + month + "-" + day + " " + hour + ":" + min + ":" + seg + "." + mil;
}

const http = require('http');
const server = http.createServer((req, res) => {
  let body = '';
  req.on('data', chunk => body += chunk);
  req.on('end', () => {
    const timestamp = GetFormattedDate(new Date());
    try {
      const content = JSON.parse(body);
      const thread = `[${content?.threadId}]`;
      const info = content.str ?? "";
      const error = content.exception ?? "";
      if (content?.level == 1) {
        console.error(timestamp, thread, info, error);
      } else if (content.level == 2) {
        console.warn(timestamp, thread, info, error);
      } else {
        console.log(timestamp, thread, info, error);
      }
    }
    catch (ex) {
      console.error(ex);
    }
    res.writeHead(200);
    res.end();
  });
});
server.listen(8080);
