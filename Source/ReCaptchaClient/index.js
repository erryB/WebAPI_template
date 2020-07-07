// Code based on article: https://medium.com/@ben.ritthichai/implementing-and-testing-recaptcha-v3-in-your-front-end-41def016d3b5
const express = require('express')
const app = express()
const port = 3000

app.get('/', function (req, res) {
  res.set('Content-Type', 'text/html');
  res.sendFile(__dirname + '/index.html');
});

app.listen(port, () => console.log(`Local Server listening on port ${port}!`))