Feature: Stock management
  As a store operator
  I want to register sales and deliveries
  So that the catalogue always shows the stock that is really available

  Scenario: Selling items reduces the available stock
    Given a product "Progressive Lens 1.60" with 10 units in stock
    When I sell 3 units of it
    Then the request succeeds
    And the product has 7 units in stock

  Scenario: Selling the last unit leaves the product out of stock
    Given a product "Titanium Frame" with 1 units in stock
    When I sell 1 units of it
    Then the request succeeds
    And the product has 0 units in stock

  Scenario: Selling more than is available is rejected
    Given a product "Sport Sunglasses" with 2 units in stock
    When I sell 5 units of it
    Then the request is rejected with status 409
    And the product has 2 units in stock

  Scenario: A delivery increases the available stock
    Given a product "Daily Contact Lenses" with 0 units in stock
    When I receive a delivery of 30 units of it
    Then the request succeeds
    And the product has 30 units in stock

  Scenario Outline: Invalid quantities are rejected
    Given a product "Lens Cleaning Spray" with 5 units in stock
    When I <movement> <quantity> units of it
    Then the request is rejected with status 400
    And the product has 5 units in stock

    Examples:
      | movement                 | quantity |
      | sell                     | 0        |
      | receive a delivery of    | 0        |
