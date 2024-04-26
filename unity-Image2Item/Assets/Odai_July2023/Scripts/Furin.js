const interval_min = 5;
const interval_max = 10;
const volume = 0.25;

const se = $.audio("Furin");

function randomInterval()
{
    return Math.random() * (interval_max - interval_min) + interval_min;
}

$.onUpdate(deltaTime => {
    if (!$.state.initialized) {
        $.state.initialized = true;
        $.state.interval = randomInterval();
    }

    let interval = $.state.interval - deltaTime;
    if(interval <= 0) {
        interval = randomInterval();
        se.volume = volume;
        se.play();
    }

    $.state.interval = interval;
});