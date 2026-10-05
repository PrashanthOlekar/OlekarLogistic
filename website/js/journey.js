/* ============ THE NH 48 RUN ============ */
// As the journey section scrolls past, the lorry drives down the road, each milestone it passes
// lights up its step, and the green sign counts the kilometres left to Hubballi.

const ROUTE_KM = 412;

function initJourney() {
  const road = document.querySelector('.journey-road');
  const lorry = document.getElementById('journeyLorry');
  const steps = [...document.querySelectorAll('#journeySteps li')];
  const kmLeft = document.getElementById('kmLeft');
  const signStep = document.getElementById('signStep');
  let queued = false;

  function update() {
    queued = false;
    const roadBox = road.getBoundingClientRect();
    const lorryHeight = lorry.offsetHeight;
    const travel = Math.max(1, roadBox.height - lorryHeight);
    // The lorry's nose follows a line 60% down the screen.
    const progress = Math.min(1, Math.max(0, (window.innerHeight * 0.6 - roadBox.top - lorryHeight) / travel));
    const y = progress * travel;
    lorry.style.transform = `translateY(${y}px)`;
    kmLeft.textContent = String(Math.round(ROUTE_KM * (1 - progress)));

    const noseY = roadBox.top + y + lorryHeight;
    let current = steps[0];
    steps.forEach((step) => {
      const reached = step.getBoundingClientRect().top <= noseY;
      step.classList.toggle('reached', reached);
      if (reached) current = step;
    });
    signStep.textContent = current.querySelector('h3').textContent;
  }

  function queue() {
    if (!queued) {
      queued = true;
      requestAnimationFrame(update);
    }
  }

  addEventListener('scroll', queue, { passive: true });
  addEventListener('resize', queue);
  update();
}
