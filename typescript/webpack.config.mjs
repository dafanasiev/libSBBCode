import path from "path";

export default {
  // Sets the mode to 'development' or 'production'
  // 'production' enables built-in optimizations like minification
  mode: "development",

  // The entry point(s) of your application
  entry: "./lib/index.js",

  // Defines where the bundled output should be placed
  output: {
    filename: "index.js", // The name of the output bundle
    path: path.resolve(import.meta.dirname, "dist"), // The output directory
    clean: true, // Cleans the 'dist' folder before each build
  },
};
