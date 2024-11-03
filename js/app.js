import {Renderer} from 'interactive-shader-format'

async function loadFile(src, callback) {
  const response = await fetch('shaders/' + src);
  const body = await response.text();

  callback(body);
}

function createRendering(fsFilename, vsFilename, label, values={}) {
  let fsSrc;
  const fsLoaded = (response) => {
    fsSrc = response;

    if (vsFilename) {
      loadFile(vsFilename, vsLoaded);
    } else {
      vsLoaded();
    }
  }

  const vsLoaded = (vsSrc) => {
    const container = document.createElement('div');
    const canvas = document.createElement('canvas');
    const title = document.createElement('div');

    title.style.position = 'absolute';
    title.style.top = '0'
    title.style.color = 'white';
    title.style.left = '0'

    container.style.position = 'relative';
    container.appendChild(canvas);
    container.appendChild(title);

    title.textContent = fsFilename;

    if (label) {
      title.textContent += '(' + label + ')'
    }

    canvas.width = window.innerWidth *.45;
    canvas.height = window.innerHeight *.45;
    document.body.appendChild(container);

    // Using webgl2 for non-power-of-two textures
    const gl = canvas.getContext('webgl2');
    const renderer = new Renderer(gl);
    renderer.loadSource(fsSrc, vsSrc);

    const animate = () => {
      requestAnimationFrame(animate);

      for (const [key, value] of Object.entries(values)) {
        renderer.setValue(key, value)
      }

      renderer.draw(canvas);
    };

    requestAnimationFrame(animate);
  }

  loadFile(fsFilename, fsLoaded);
}

createRendering('cubes.fs', undefined, 'Full Chroma');

createRendering('cubes.fs', undefined, 'Desaturated', {
  saturation: 0.2
});
