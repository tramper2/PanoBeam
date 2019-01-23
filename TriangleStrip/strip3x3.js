
function setup() {
    createCanvas(1000, 580);
    background(204);
}

function draw() {
    background(204);

    var indexes = [
        0,
        3,
        1,
        4,
        2,
        5,
        5,
        8,
        4,
        7,
        3,
        6];
    var points = [[0, 0], [1320, 0], [1920, 0],
        [0, 540], [1320, 540], [1920, 540],
        [0, 1080], [1320, 1080],  [1920, 1080]];

    beginShape(TRIANGLE_STRIP);
    for (var i = 0; i < indexes.length; i++) {
        var x = points[indexes[i]][0] / 2 + 20;
        var y = points[indexes[i]][1] / 2 + 20;
        vertex(x, y);
    }
    endShape();
}