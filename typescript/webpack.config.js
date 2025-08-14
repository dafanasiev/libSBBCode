const path = require('path');

module.exports = {
  // Sets the mode to 'development' or 'production'
  // 'production' enables built-in optimizations like minification
  mode: 'development', 

  // The entry point(s) of your application
  entry: './lib/index.js', 

  // Defines where the bundled output should be placed
  output: {
    filename: 'bundle.js', // The name of the output bundle
    path: path.resolve(__dirname, 'dist'), // The output directory
    clean: true, // Cleans the 'dist' folder before each build
  },
};